using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int GustRange = 3;
    public const int GustDistance = 3;

    private void DriftCreatures(List<GameEvent> events)
    {
        if (!Player.IsDead && Map.CurrentAt(Player.Position) is { } flow)
        {
            var target = Player.Position.Move(flow);

            if (IsFree(target))
            {
                Player.Position = target;
                events.Add(new PlayerDrifted(target));
                PickUpItemAt(target, events);
            }
        }

        foreach (var enemy in _enemies.Where(enemy => !enemy.IsDead && enemy.Kind.RidesCurrents).ToList())
        {
            if (Map.CurrentAt(enemy.Position) is not { } current)
                continue;

            var target = enemy.Position.Move(current);

            if (!CanEnemyEnter(enemy, target))
                continue;

            enemy.Position = target;
            events.Add(new EnemyDrifted(enemy.Id, target));
        }
    }

    private void BlowAway(Enemy enemy, List<GameEvent> events)
    {
        var distance = Distance(enemy.Position, Player.Position);

        if (distance is < 1 or > GustRange)
            return;

        var direction = AwayFrom(enemy.Position, Player.Position);
        var moved = 0;

        while (moved < GustDistance && IsFree(Player.Position.Move(direction)))
        {
            Player.Position = Player.Position.Move(direction);
            moved++;
        }

        var damage = moved < GustDistance ? LavaDamage(Player.Stats.MaxHealth) : 0;
        Player.TakeDamage(damage);
        events.Add(new PlayerBlownAway(enemy.Id, Player.Position, damage));

        if (moved > 0)
            PickUpItemAt(Player.Position, events);
    }

    private static Direction AwayFrom(Position from, Position to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx >= 0 ? Direction.Right : Direction.Left;

        return dy > 0 ? Direction.Down : Direction.Up;
    }
}
