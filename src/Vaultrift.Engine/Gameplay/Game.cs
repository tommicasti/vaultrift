using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int EnemyWakeDistance = 5;
    public const int HealthRegenInterval = 3;
    public const int HealthRegenAmount = 1;

    private readonly List<Enemy> _enemies;
    private readonly Dictionary<Position, ItemInstance> _floorItems = [];
    private readonly DamageCalculator _damageCalculator;
    private readonly IRandom _random;
    private readonly Position _playerStart;
    private int _stepsThisTurn;

    public Game(
        DungeonMap map,
        Player player,
        IEnumerable<Enemy> enemies,
        IRandom random,
        IEnumerable<FloorItem>? floorItems = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(random);

        _enemies = enemies.ToList();

        if (!map.IsWalkable(player.Position))
            throw new ArgumentException("The player must start on a floor tile.", nameof(player));

        if (_enemies.Any(enemy => !map.IsWalkable(enemy.Position)))
            throw new ArgumentException("Every enemy must start on a floor tile.", nameof(enemies));

        var occupied = _enemies.Select(enemy => enemy.Position).Append(player.Position).ToList();
        if (occupied.Distinct().Count() != occupied.Count)
            throw new ArgumentException("Two creatures cannot start on the same tile.", nameof(enemies));

        if (_enemies.Select(enemy => enemy.Id).Distinct().Count() != _enemies.Count)
            throw new ArgumentException("Enemy ids must be unique.", nameof(enemies));

        foreach (var floorItem in floorItems ?? Enumerable.Empty<FloorItem>())
        {
            if (!map.IsWalkable(floorItem.Position))
                throw new ArgumentException("Every item must lie on a floor tile.", nameof(floorItems));

            if (!_floorItems.TryAdd(floorItem.Position, floorItem.Item))
                throw new ArgumentException("Two items cannot lie on the same tile.", nameof(floorItems));
        }

        Map = map;
        Player = player;
        _playerStart = player.Position;
        _random = random;
        _damageCalculator = new DamageCalculator(random);
    }

    public DungeonMap Map { get; }

    public Player Player { get; }

    public IReadOnlyList<Enemy> Enemies => _enemies;

    public IReadOnlyDictionary<Position, ItemInstance> FloorItems => _floorItems;

    public int Deaths { get; private set; }

    public int TurnNumber { get; private set; }

    public int FreeStepsLeft => Player.StepsPerTurn - 1 - _stepsThisTurn;

    public TurnResult MovePlayer(Direction direction)
    {
        var events = new List<GameEvent>();
        Player.Facing = direction;

        var target = Player.Position.Move(direction);

        if (!IsFree(target))
        {
            events.Add(new PlayerTurned(direction));
            return new TurnResult(TurnPassed: false, events);
        }

        Player.Position = target;
        events.Add(new PlayerMoved(target));
        PickUpItemAt(target, events);

        if (FreeStepsLeft > 0)
        {
            _stepsThisTurn++;
            return new TurnResult(TurnPassed: false, events);
        }

        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult Attack()
    {
        var events = new List<GameEvent>();
        var weapon = Player.Weapon;

        switch ((weapon?.Definition as WeaponDefinition)?.WeaponType)
        {
            case WeaponType.Bow:
                Shoot(weapon!, events);
                break;
            case WeaponType.Axe:
                Sweep(weapon!, events);
                break;
            default:
                Strike(weapon, events);
                break;
        }

        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult Equip(int entryId)
    {
        var events = new List<GameEvent>();
        var result = Player.Equip(entryId);

        if (result != EquipResult.Equipped)
        {
            events.Add(new EquipRejected(result));
            return new TurnResult(TurnPassed: false, events);
        }

        events.Add(new ItemEquipped(Player.Inventory.Find(entryId)!.Item));

        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult UseItem(int entryId)
    {
        var item = Player.Inventory.Find(entryId)?.Item;
        return ResolveUse(item, Player.Use(entryId));
    }

    public TurnResult UseQuickSlot()
    {
        var item = Player.QuickSlot is { } potion ? new ItemInstance(potion) : null;
        return ResolveUse(item, Player.UseQuickSlot());
    }

    public TurnResult AssignQuickSlot(int entryId)
    {
        var item = Player.Inventory.Find(entryId)?.Item.Definition;

        GameEvent outcome = Player.AssignQuickSlot(entryId)
            ? new QuickSlotAssigned(item!)
            : new UseRejected(item is null ? UseResult.ItemNotFound : UseResult.NotUsable);

        return new TurnResult(TurnPassed: false, [outcome]);
    }

    private TurnResult ResolveUse(ItemInstance? item, UseResult result)
    {
        var events = new List<GameEvent>();

        if (result != UseResult.Used)
        {
            events.Add(new UseRejected(result));
            return new TurnResult(TurnPassed: false, events);
        }

        events.Add(new ItemUsed(item!));

        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    private void PickUpItemAt(Position position, List<GameEvent> events)
    {
        if (!_floorItems.TryGetValue(position, out var item))
            return;

        if (Player.Inventory.TryAdd(item))
        {
            _floorItems.Remove(position);
            events.Add(new ItemPickedUp(item));
        }
        else
        {
            events.Add(new InventoryFull(item));
        }
    }

    private void EndTurn(List<GameEvent> events)
    {
        _stepsThisTurn = 0;
        EnemiesAct(events);
        BurnEnemies(events);
        TurnNumber++;

        if (Player.IsDead)
        {
            RespawnPlayer(events);
            return;
        }

        foreach (var boost in Player.AdvanceBoosts())
            events.Add(new BoostExpired(boost.Source));

        if (TurnNumber % HealthRegenInterval == 0)
            Player.Heal(HealthRegenAmount);
    }

    private void EnemiesAct(List<GameEvent> events)
    {
        foreach (var enemy in _enemies)
        {
            if (Player.IsDead)
                break;

            if (enemy.IsDead)
                continue;

            if (enemy.FreezeImmunityTurnsLeft > 0)
                enemy.FreezeImmunityTurnsLeft--;

            if (!enemy.IsAwake)
            {
                if (Distance(enemy.Position, Player.Position) > EnemyWakeDistance)
                    continue;

                enemy.IsAwake = true;
                events.Add(new EnemyWokeUp(enemy.Id));
            }

            if (enemy.IsFrozen)
            {
                ThawOneTurn(enemy, events);
                continue;
            }

            if (enemy.IsResting)
            {
                enemy.IsResting = false;
                continue;
            }

            TakeEnemyTurn(enemy, events);
            enemy.IsResting = enemy.Kind.Speed == Speed.Slow;
        }
    }

    private void TakeEnemyTurn(Enemy enemy, List<GameEvent> events)
    {
        if (IsNextToPlayer(enemy))
        {
            AttackPlayer(enemy, events);
            return;
        }

        for (var step = 0; step < enemy.StepsPerTurn; step++)
        {
            if (!StepTowardPlayer(enemy, events) || IsNextToPlayer(enemy))
                return;
        }
    }

    private void AttackPlayer(Enemy enemy, List<GameEvent> events)
    {
        var result = ElementRules.Apply(
            _damageCalculator.Calculate(enemy.CombatStats, Player.CombatStats),
            enemy.Kind.AttackElement,
            weakness: null,
            Player.Resistance,
            enemy.Rarity);

        Player.TakeDamage(result.Damage);
        events.Add(new EnemyAttackedPlayer(enemy.Id, result));
    }

    private void RespawnPlayer(List<GameEvent> events)
    {
        Deaths++;
        events.Add(new PlayerDefeated());

        foreach (var enemy in _enemies.Where(enemy => !enemy.IsDead))
        {
            enemy.Position = enemy.StartPosition;
            enemy.IsAwake = false;
            enemy.IsResting = false;
            enemy.ClearStatuses();
            enemy.RestoreFullHealth();
        }

        Player.Position = _playerStart;
        Player.Facing = Direction.Down;
        Player.RestoreFullHealth();
        Player.ClearBoosts();
        events.Add(new PlayerRespawned(_playerStart));
    }

    private bool StepTowardPlayer(Enemy enemy, List<GameEvent> events)
    {
        foreach (var direction in ApproachDirections(enemy.Position, Player.Position))
        {
            var target = enemy.Position.Move(direction);

            if (!CanEnemyEnter(enemy, target))
                continue;

            enemy.Position = target;
            events.Add(new EnemyMoved(enemy.Id, target));
            return true;
        }

        return false;
    }

    private static IEnumerable<Direction> ApproachDirections(Position from, Position to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;

        var horizontal = dx > 0 ? Direction.Right : Direction.Left;
        var vertical = dy > 0 ? Direction.Down : Direction.Up;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx != 0)
                yield return horizontal;

            if (dy != 0)
                yield return vertical;
        }
        else
        {
            yield return vertical;

            if (dx != 0)
                yield return horizontal;
        }
    }

    private bool IsFree(Position position) =>
        Map.IsWalkable(position) &&
        position != Player.Position &&
        FindLivingEnemyAt(position) is null;

    private bool CanEnemyEnter(Enemy enemy, Position position) =>
        (enemy.Kind.PassesThroughWalls ? Map.IsInside(position) : Map.IsWalkable(position)) &&
        position != Player.Position &&
        FindLivingEnemyAt(position) is null;

    private bool IsNextToPlayer(Enemy enemy) =>
        Distance(enemy.Position, Player.Position) == 1;

    private Enemy? FindLivingEnemyAt(Position position) =>
        _enemies.FirstOrDefault(enemy => !enemy.IsDead && enemy.Position == position);

    private static int Distance(Position a, Position b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}