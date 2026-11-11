using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Game
{
    public const int EnemyWakeDistance = 5;

    private readonly List<Enemy> _enemies;
    private readonly DamageCalculator _damageCalculator;

    public Game(DungeonMap map, Player player, IEnumerable<Enemy> enemies, IRandom random)
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

        Map = map;
        Player = player;
        _damageCalculator = new DamageCalculator(random);
    }

    public DungeonMap Map { get; }

    public Player Player { get; }

    public IReadOnlyList<Enemy> Enemies => _enemies;

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

        EnemiesAct(events);
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult Attack()
    {
        var events = new List<GameEvent>();
        var target = FindLivingEnemyAt(Player.Position.Move(Player.Facing));

        if (target is null)
        {
            events.Add(new PlayerAttackedNothing());
        }
        else
        {
            var result = _damageCalculator.Calculate(Player.Stats, target.Stats);
            target.TakeDamage(result.Damage);
            events.Add(new PlayerAttackedEnemy(target.Id, result));

            if (target.IsDead)
                events.Add(new EnemyDefeated(target.Id));
        }

        EnemiesAct(events);
        return new TurnResult(TurnPassed: true, events);
    }

    private void EnemiesAct(List<GameEvent> events)
    {
        foreach (var enemy in _enemies)
        {
            if (Player.IsDead)
                break;

            if (enemy.IsDead)
                continue;

            if (!enemy.IsAwake)
            {
                if (Distance(enemy.Position, Player.Position) > EnemyWakeDistance)
                    continue;

                enemy.IsAwake = true;
                events.Add(new EnemyWokeUp(enemy.Id));
            }

            if (Distance(enemy.Position, Player.Position) == 1)
                AttackPlayer(enemy, events);
            else
                MoveTowardPlayer(enemy, events);
        }
    }

    private void AttackPlayer(Enemy enemy, List<GameEvent> events)
    {
        var result = _damageCalculator.Calculate(enemy.Stats, Player.Stats);
        Player.TakeDamage(result.Damage);
        events.Add(new EnemyAttackedPlayer(enemy.Id, result));
    }

    private void MoveTowardPlayer(Enemy enemy, List<GameEvent> events)
    {
        foreach (var direction in ApproachDirections(enemy.Position, Player.Position))
        {
            var target = enemy.Position.Move(direction);

            if (!IsFree(target))
                continue;

            enemy.Position = target;
            events.Add(new EnemyMoved(enemy.Id, target));
            return;
        }
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

    private Enemy? FindLivingEnemyAt(Position position) =>
        _enemies.FirstOrDefault(enemy => !enemy.IsDead && enemy.Position == position);

    private static int Distance(Position a, Position b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}