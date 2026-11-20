using System.Diagnostics.CodeAnalysis;
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
    public const int BossPursuitDistance = 3;
    public const int BossPursuitSteps = 2;

    private readonly IFloorProvider _floors;
    private readonly Dictionary<Position, ItemInstance> _floorItems = [];
    private readonly DamageCalculator _damageCalculator;
    private readonly IRandom _random;
    private readonly ILootTable? _loot;
    private List<Enemy> _enemies = [];
    private Position _playerStart;
    private int _stepsThisTurn;
    private bool _bossDefeatAnnounced;

    public Game(
        DungeonMap map,
        Player player,
        IEnumerable<Enemy> enemies,
        IRandom random,
        IEnumerable<FloorItem>? floorItems = null)
        : this(player, SingleFloor.From(map, player, enemies, floorItems), random)
    {
    }

    public Game(Player player, IFloorProvider floors, IRandom random, ILootTable? loot = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(floors);
        ArgumentNullException.ThrowIfNull(random);

        Player = player;
        _floors = floors;
        _random = random;
        _loot = loot;
        _damageCalculator = new DamageCalculator(random);

        LoadFloor(1);
    }

    public DungeonMap Map { get; private set; }

    public Player Player { get; }

    public IReadOnlyList<Enemy> Enemies => _enemies;

    public IReadOnlyDictionary<Position, ItemInstance> FloorItems => _floorItems;

    public int Deaths { get; private set; }

    public int TurnNumber { get; private set; }

    public int FreeStepsLeft => Player.StepsPerTurn - 1 - _stepsThisTurn;

    public int FloorNumber { get; private set; }

    public int FloorCount => _floors.Count;

    public Position? Stairs { get; private set; }

    public string? Biome { get; private set; }

    public bool StairsOpen => Stairs is not null && FloorNumber < FloorCount && BossesDefeated;

    public bool IsConquered { get; private set; }

    private bool HasBoss => _enemies.Any(enemy => enemy.Kind.IsBoss);

    private bool BossesDefeated => _enemies.Where(enemy => enemy.Kind.IsBoss).All(enemy => enemy.IsDead);

    public TurnResult MovePlayer(Direction direction)
    {
        if (IsLost)
            return Defeated();

        var events = new List<GameEvent>();
        Player.Facing = direction;

        var target = Player.Position.Move(direction);

        if (IsMerchant(target))
            return new TurnResult(TurnPassed: false, [new MerchantGreeted()]);

        if (!IsFree(target))
        {
            events.Add(new PlayerTurned(direction));
            return new TurnResult(TurnPassed: false, events);
        }

        Player.Position = target;
        events.Add(new PlayerMoved(target));
        PickUpItemAt(target, events);
        RecoverGraveAt(target, events);

        if (target == Stairs)
        {
            if (StairsOpen)
            {
                Descend(events);
                return new TurnResult(TurnPassed: true, events);
            }

            events.Add(new StairsSealed());
        }

        if (FreeStepsLeft > 0)
        {
            _stepsThisTurn++;
            return new TurnResult(TurnPassed: false, events);
        }

        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult Face(Direction direction)
    {
        if (IsLost)
            return Defeated();

        Player.Facing = direction;
        return new TurnResult(TurnPassed: false, [new PlayerTurned(direction)]);
    }

    public TurnResult Attack()
    {
        if (IsLost)
            return Defeated();

        if (IsMerchant(Player.Position.Move(Player.Facing)))
            return new TurnResult(TurnPassed: false, [new MerchantGreeted()]);

        var events = new List<GameEvent>();
        PerformAttack(events);
        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult Equip(int entryId, EquipmentSlot? hand = null)
    {
        var events = new List<GameEvent>();
        var item = Player.Inventory.Find(entryId)?.Item;
        var result = Player.Equip(entryId, hand);

        if (result != EquipResult.Equipped)
        {
            events.Add(new EquipRejected(result));
            return new TurnResult(TurnPassed: false, events);
        }

        events.Add(new ItemEquipped(item!));

        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult Unequip(EquipmentSlot slot)
    {
        var events = new List<GameEvent>();
        var item = Player.Equipment.TryGetValue(slot, out var entry) ? entry.Item : null;
        var result = Player.Unequip(slot);

        if (result != UnequipResult.Unequipped)
        {
            events.Add(new UnequipRejected(result));
            return new TurnResult(TurnPassed: false, events);
        }

        events.Add(new ItemUnequipped(item!));

        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult SpendPoint(StatPoint stat)
    {
        var result = Player.SpendPoint(stat);

        GameEvent outcome = result == SpendResult.Spent
            ? new StatPointSpent(stat)
            : new SpendRejected(result);

        return new TurnResult(TurnPassed: false, [outcome]);
    }

    public TurnResult UseItem(int entryId)
    {
        var entry = Player.Inventory.Find(entryId);

        if (entry?.Item.Definition is ScrollDefinition)
            return new TurnResult(TurnPassed: false, [new UseRejected(UseResult.ScrollNeedsSlot)]);

        return ResolveUse(entry?.Item, Player.Use(entryId));
    }

    public TurnResult UseQuickSlot(int slot = 0)
    {
        if (Player.QuickSlotEntry(slot) is { Item.Definition: ScrollDefinition scroll } scrollEntry)
            return ReadScroll(scrollEntry, scroll);

        var item = Player.QuickSlots.ElementAtOrDefault(slot) is { } definition ? new ItemInstance(definition) : null;
        return ResolveUse(item, Player.UseQuickSlot(slot));
    }

    public TurnResult AssignQuickSlot(int entryId, int slot = 0)
    {
        var item = Player.Inventory.Find(entryId)?.Item.Definition;

        GameEvent outcome = Player.AssignQuickSlot(entryId, slot)
            ? new QuickSlotAssigned(item!, slot)
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

        if (item!.Definition is RuneDefinition && Player.Weapon is { } imbued)
            events.Add(new WeaponImbued(imbued));

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
        if (IsLost)
            return;

        _stepsThisTurn = 0;
        CoolLava(events);
        EnemiesAct(events);
        DriftCreatures(events);
        BurnEnemies(events);
        ScorchCreatures(events);
        BreatheSpores(events);
        SufferPoison(events);
        AnnounceBossDefeat(events);
        TurnNumber++;

        if (Player.IsDead)
        {
            RespawnPlayer(events);
            return;
        }

        foreach (var boost in Player.AdvanceBoosts())
            events.Add(new BoostExpired(boost.Source));

        TickAbilities(events);
        Regenerate();
        RespawnMonster(events);
    }

    private void Defeat(Enemy enemy, List<GameEvent> events)
    {
        events.Add(new EnemyDefeated(enemy.Id));

        if (enemy.Kind.Minion is not null)
            WitherMinions(enemy, events);

        if (enemy.IsSummoned)
            return;

        EarnCoins(Progression.CoinsFor(enemy), events);

        var experience = Progression.ExperienceFor(enemy, Player.EffectAmount(EffectKind.Wisdom), Player.Level);
        var levelBefore = Player.Level;
        events.Add(new ExperienceGained(experience));

        Player.GainExperience(experience);

        for (var level = levelBefore + 1; level <= Player.Level; level++)
            events.Add(new LeveledUp(level));

        if (Player.FuryTurns > 0)
            Player.FuryTurns += Player.EffectAmount(EffectKind.FuryExtension);

        SiphonMana(events);

        DropLoot(enemy, events);
    }

    private void Descend(List<GameEvent> events)
    {
        _stepsThisTurn = 0;
        LoadFloor(FloorNumber + 1, events);
        Player.Facing = Direction.Down;
        Player.RestoreFullHealth();
        Player.RestoreFullMana();
        Player.PoisonTurns = 0;
        events.Add(new FloorDescended(FloorNumber, Player.Position));
    }

    private void AnnounceBossDefeat(List<GameEvent> events)
    {
        if (_bossDefeatAnnounced || !HasBoss || !BossesDefeated)
            return;

        _bossDefeatAnnounced = true;

        if (FloorNumber == FloorCount)
        {
            IsConquered = true;
            events.Add(new VaultConquered());
        }
        else if (Stairs is { } stairs)
        {
            events.Add(new StairsOpened(stairs));
        }
    }

    [MemberNotNull(nameof(Map))]
    private void LoadFloor(int number, List<GameEvent>? events = null)
    {
        if (events is not null)
            AbandonGrave(events);

        var floor = _floors.Create(number) ?? throw new InvalidOperationException($"Floor {number} could not be created.");
        Validate(floor);

        Map = floor.Map;
        _enemies = floor.Enemies.ToList();
        _floorItems.Clear();

        foreach (var floorItem in floor.Items)
            _floorItems.Add(floorItem.Position, floorItem.Item);

        Player.Position = floor.Start;
        _playerStart = floor.Start;
        Stairs = floor.Stairs;
        Biome = floor.Biome;
        LoadChests(floor);
        Merchant = floor.Merchant;
        _coolingLava.Clear();
        FloorNumber = number;
        _bossDefeatAnnounced = false;
    }

    private static void Validate(Floor floor)
    {
        var map = floor.Map;

        if (!map.IsWalkable(floor.Start))
            throw new ArgumentException("The player must start on a floor tile.", "player");

        if (floor.Enemies.Any(enemy => !map.IsWalkable(enemy.Position)))
            throw new ArgumentException("Every enemy must start on a floor tile.", "enemies");

        var occupied = floor.Enemies.Select(enemy => enemy.Position).Append(floor.Start).ToList();
        if (occupied.Distinct().Count() != occupied.Count)
            throw new ArgumentException("Two creatures cannot start on the same tile.", "enemies");

        if (floor.Enemies.Select(enemy => enemy.Id).Distinct().Count() != floor.Enemies.Count)
            throw new ArgumentException("Enemy ids must be unique.", "enemies");

        if (floor.Items.Any(item => !map.IsWalkable(item.Position)))
            throw new ArgumentException("Every item must lie on a floor tile.", "floorItems");

        if (floor.Items.Select(item => item.Position).Distinct().Count() != floor.Items.Count)
            throw new ArgumentException("Two items cannot lie on the same tile.", "floorItems");

        if (floor.Stairs is { } stairs && !map.IsWalkable(stairs))
            throw new ArgumentException("The stairs must be on a floor tile.", "floor");

        var chests = floor.Chests ?? [];
        var taken = occupied
            .Concat(floor.Items.Select(item => item.Position))
            .Concat(floor.Stairs is { } stairsTile ? [stairsTile] : [])
            .ToHashSet();

        if (chests.Any(chest => !map.IsWalkable(chest.Position) || taken.Contains(chest.Position)))
            throw new ArgumentException("A chest must stand alone on a floor tile.", "floor");

        if (chests.Select(chest => chest.Position).Distinct().Count() != chests.Count)
            throw new ArgumentException("Two chests cannot share a tile.", "floor");

        if (floor.Merchant is { } merchant &&
            (!map.IsWalkable(merchant.Position) || taken.Contains(merchant.Position) || chests.Any(chest => chest.Position == merchant.Position)))
            throw new ArgumentException("The merchant must stand alone on a floor tile.", "floor");

        if (chests.Any(chest => chest.Mimic is { } mimic && mimic.Position != chest.Position))
            throw new ArgumentException("A mimic must hide inside its chest.", "floor");

        var ids = floor.Enemies.Select(enemy => enemy.Id)
            .Concat(chests.Select(chest => chest.Mimic?.Id).OfType<int>())
            .ToList();

        if (ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("Mimic ids must be unique.", "floor");
    }

    private void EnemiesAct(List<GameEvent> events)
    {
        foreach (var enemy in _enemies.ToList())
        {
            if (Player.IsDead)
                break;

            if (enemy.IsDead)
                continue;

            if (enemy.FreezeImmunityTurnsLeft > 0)
                enemy.FreezeImmunityTurnsLeft--;

            if (Player.IsInvisible)
                continue;

            if (!enemy.IsAwake)
            {
                if (Distance(enemy.Position, Player.Position) > WakeDistance)
                    continue;

                enemy.IsAwake = true;
                events.Add(new EnemyWokeUp(enemy.Id));
            }

            if (enemy.IsFrozen)
            {
                ThawOneTurn(enemy, events);
                continue;
            }

            if (enemy.IsResting && !Pursues(enemy))
            {
                enemy.IsResting = false;
                continue;
            }

            TakeEnemyTurn(enemy, events);
            enemy.IsResting = enemy.Speed == Speed.Slow;
        }
    }

    private void TakeEnemyTurn(Enemy enemy, List<GameEvent> events)
    {
        UseBossPower(enemy, events);

        if (IsNextToPlayer(enemy))
        {
            AttackPlayer(enemy, events);
            return;
        }

        if (enemy.Kind.IsStationary)
            return;

        var steps = Pursues(enemy) ? Math.Max(BossPursuitSteps, enemy.StepsPerTurn) : enemy.StepsPerTurn;

        for (var step = 0; step < steps; step++)
        {
            if (!StepTowardPlayer(enemy, events) || IsNextToPlayer(enemy))
                return;
        }
    }

    private bool Pursues(Enemy enemy) =>
        enemy.Kind.IsBoss && !enemy.Kind.IsStationary && Distance(enemy.Position, Player.Position) >= BossPursuitDistance;

    private void AttackPlayer(Enemy enemy, List<GameEvent> events)
    {
        var defense = Player.CombatStats;
        var result = ApplyBulwark(ChargeInWater(ElementRules.Apply(
            _damageCalculator.Calculate(enemy.CombatStats, defense),
            enemy.Kind.AttackElement,
            weakness: null,
            Player.Resistance,
            enemy.Rarity), Player));

        Player.TakeDamage(result.Damage);
        events.Add(new EnemyAttackedPlayer(enemy.Id, result));

        if (result.IsBlocked && Player.EffectAmount(EffectKind.BlockReflect) > 0)
            Prick(enemy, DamageCalculator.BaseDamage(enemy.CombatStats, defense), events);

        var thorns = Player.EffectAmount(EffectKind.Thorns);

        if (!result.IsDodged && thorns > 0)
            Prick(enemy, thorns, events);
    }

    private AttackResult ApplyBulwark(AttackResult result)
    {
        if (Player.BulwarkTurns == 0 || result.Missed)
            return result;

        return Player.HasShield
            ? AttackResult.Blocked with { Element = result.Element }
            : result with { Damage = Math.Max(DamageCalculator.MinimumDamage, result.Damage / 2) };
    }

    private void Prick(Enemy enemy, int damage, List<GameEvent> events)
    {
        if (enemy.IsDead)
            return;

        enemy.TakeDamage(damage);
        events.Add(new EnemyPricked(enemy.Id, damage));

        if (enemy.IsDead)
            Defeat(enemy, events);
    }

    private void RespawnPlayer(List<GameEvent> events)
    {
        if (HandleDeath(events))
            return;

        Deaths++;
        events.Add(new PlayerDefeated());

        if (LeavesGraves)
            DigGrave(events);

        DismissMinions();

        if (!LeavesGraves)
            LoseCoins(events);

        foreach (var enemy in _enemies.Where(enemy => !enemy.IsDead))
            enemy.ReturnToStart();

        Player.Position = _playerStart;
        Player.Facing = Direction.Down;
        Player.RestoreFullHealth();
        Player.RestoreFullMana();
        Player.ClearBoosts();
        Player.ClearStatuses();
        events.Add(new PlayerRespawned(_playerStart));
        TakeSpareWeapon(events);
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
        !IsChest(position) &&
        !IsMerchant(position) &&
        position != Player.Position &&
        FindLivingEnemyAt(position) is null;

    private bool CanEnemyEnter(Enemy enemy, Position position) =>
        (enemy.Kind.PassesThroughWalls ? Map.IsInside(position) : Map.IsWalkable(position)) &&
        !IsChest(position) &&
        !IsMerchant(position) &&
        !AvoidsLava(enemy, position) &&
        position != Player.Position &&
        FindLivingEnemyAt(position) is null;

    private bool IsNextToPlayer(Enemy enemy) =>
        Distance(enemy.Position, Player.Position) == 1;

    private Enemy? FindLivingEnemyAt(Position position) =>
        _enemies.FirstOrDefault(enemy => !enemy.IsDead && enemy.Position == position);

    private static int Distance(Position a, Position b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}