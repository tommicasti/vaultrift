using Vaultrift.Engine.Combat;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    private bool TryShatterCrystal(List<GameEvent> events)
    {
        var front = Player.Position.Move(Player.Facing);

        if (!Map.IsCrystal(front) || FindLivingEnemyAt(front) is not null)
            return false;

        Shatter(front, events);
        return true;
    }

    private bool TrySmashObstacle(List<GameEvent> events)
    {
        var front = Player.Position.Move(Player.Facing);

        if (Map.GetTile(front) != TileType.Obstacle || FindLivingEnemyAt(front) is not null)
            return false;

        Map.SetTile(front, TileType.Floor);
        events.Add(new ObstacleSmashed(front));
        return true;
    }

    private void Shatter(Position crystal, List<GameEvent> events)
    {
        Map.SetTile(crystal, TileType.Floor);
        events.Add(new CrystalShattered(crystal));

        var victims = _enemies
            .Where(enemy => !enemy.IsDead && IsAround(crystal, enemy.Position))
            .ToList();

        foreach (var enemy in victims)
        {
            var damage = DamageCalculator.BaseDamage(Player.CombatStats, enemy.CombatStats);
            enemy.TakeDamage(damage);
            events.Add(new EnemyShardHit(enemy.Id, damage));

            if (enemy.IsDead)
                Defeat(enemy, events);
        }
    }

    private void BloomCrystals(Enemy enemy, List<GameEvent> events)
    {
        var around = Around(enemy.Position).Where(Map.IsInside).ToList();
        var crystals = around.Where(Map.IsCrystal).ToList();

        if (crystals.Count > 0)
        {
            BurstCrystals(enemy, crystals, events);
            return;
        }

        var tiles = around.Where(CanGrowCrystal).ToList();

        if (tiles.Count == 0)
            return;

        foreach (var tile in tiles)
            Map.SetTile(tile, TileType.Crystal);

        events.Add(new CrystalsGrew(enemy.Id, tiles));
    }

    private void BurstCrystals(Enemy enemy, List<Position> crystals, List<GameEvent> events)
    {
        foreach (var crystal in crystals)
            Map.SetTile(crystal, TileType.Floor);

        events.Add(new CrystalsBurst(enemy.Id, crystals));

        if (!crystals.Any(crystal => IsAround(crystal, Player.Position)))
            return;

        var damage = DamageCalculator.BaseDamage(enemy.CombatStats, Player.CombatStats);
        Player.TakeDamage(damage);
        events.Add(new PlayerShardHit(damage));
    }

    private bool CanGrowCrystal(Position tile) =>
        Map.GetTile(tile) == TileType.Floor &&
        tile != Stairs &&
        tile != Player.Position &&
        !IsChest(tile) &&
        !IsMerchant(tile) &&
        !_floorItems.ContainsKey(tile) &&
        FindLivingEnemyAt(tile) is null;

    private static IEnumerable<Position> Around(Position center) =>
        Surroundings.Select(offset => new Position(center.X + offset.X, center.Y + offset.Y));

    private static bool IsAround(Position center, Position other) =>
        center != other && Math.Abs(center.X - other.X) <= 1 && Math.Abs(center.Y - other.Y) <= 1;
}
