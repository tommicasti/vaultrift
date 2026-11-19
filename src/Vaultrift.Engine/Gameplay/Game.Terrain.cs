using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int PowerInterval = 3;
    public const int LavaDamagePercent = 10;
    public const int MinimumLavaDamage = 3;
    public const int PullMinDistance = 2;
    public const int PullMaxDistance = 5;
    public const int WaterLightningMultiplier = 2;
    public const int LavaCoolingTurns = 3;

    private readonly Dictionary<Position, int> _coolingLava = [];

    private static readonly Position[] Surroundings =
        [new(-1, -1), new(0, -1), new(1, -1), new(-1, 0), new(1, 0), new(-1, 1), new(0, 1), new(1, 1)];

    public static int LavaDamage(int maxHealth) =>
        Math.Max(MinimumLavaDamage, maxHealth * LavaDamagePercent / 100);

    private bool AvoidsLava(Enemy enemy, Position position) =>
        Map.IsLava(position) && !enemy.Kind.IsFireproof;

    private bool CanBePushedInto(Enemy enemy, Position position) =>
        (enemy.Kind.PassesThroughWalls ? Map.IsInside(position) : Map.IsWalkable(position)) &&
        !IsChest(position) &&
        !IsMerchant(position) &&
        position != Player.Position &&
        FindLivingEnemyAt(position) is null;

    private void ScorchCreatures(List<GameEvent> events)
    {
        if (Map.IsLava(Player.Position) && Player.Resistance != Element.Fire && !Player.IsDead)
        {
            var damage = LavaDamage(Player.Stats.MaxHealth);
            Player.TakeDamage(damage);
            events.Add(new PlayerScorched(damage));
        }

        foreach (var enemy in _enemies.Where(enemy => !enemy.IsDead && AvoidsLava(enemy, enemy.Position)).ToList())
            Scorch(enemy, events);
    }

    private void Scorch(Enemy enemy, List<GameEvent> events)
    {
        var damage = LavaDamage(enemy.Stats.MaxHealth);
        enemy.TakeDamage(damage);
        events.Add(new EnemyScorched(enemy.Id, damage));

        if (enemy.IsDead)
            Defeat(enemy, events);
    }

    private AttackResult ChargeInWater(AttackResult result, Creature target)
    {
        if (result.Missed || result.Element != Element.Lightning || !Map.IsWater(target.Position))
            return result;

        return result with
        {
            Damage = result.Damage * WaterLightningMultiplier,
            Effectiveness = Effectiveness.Effective
        };
    }

    private void UseBossPower(Enemy enemy, List<GameEvent> events)
    {
        if (enemy.Kind.Power == BossPower.None)
            return;

        enemy.PowerCharge++;

        if (enemy.PowerCharge < PowerInterval)
            return;

        enemy.PowerCharge = 0;

        switch (enemy.Kind.Power)
        {
            case BossPower.LavaRing:
                SpreadLava(enemy, events);
                break;
            case BossPower.TentaclePull:
                PullPlayer(enemy, events);
                break;
            case BossPower.CrystalBloom:
                BloomCrystals(enemy, events);
                break;
            case BossPower.SummonMinions:
                SummonMinions(enemy, events);
                break;
            case BossPower.Gust:
                BlowAway(enemy, events);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(enemy), enemy.Kind.Power, "Unknown boss power.");
        }
    }

    private void SpreadLava(Enemy enemy, List<GameEvent> events)
    {
        var tiles = Surroundings
            .Select(offset => new Position(enemy.Position.X + offset.X, enemy.Position.Y + offset.Y))
            .Where(tile => Map.IsInside(tile) && Map.GetTile(tile) == TileType.Floor)
            .Where(tile => tile != Stairs && !IsChest(tile) && !IsMerchant(tile))
            .ToList();

        if (tiles.Count == 0)
            return;

        foreach (var tile in tiles)
        {
            Map.SetTile(tile, TileType.Lava);
            _coolingLava[tile] = LavaCoolingTurns;
        }

        events.Add(new LavaSpread(enemy.Id, tiles));
    }

    private void CoolLava(List<GameEvent> events)
    {
        var cooled = new List<Position>();

        foreach (var tile in _coolingLava.Keys.ToList())
        {
            if (--_coolingLava[tile] > 0)
                continue;

            _coolingLava.Remove(tile);
            Map.SetTile(tile, TileType.Floor);
            cooled.Add(tile);
        }

        if (cooled.Count > 0)
            events.Add(new LavaCooled(cooled));
    }

    private void PullPlayer(Enemy enemy, List<GameEvent> events)
    {
        var distance = Distance(enemy.Position, Player.Position);

        if (distance is < PullMinDistance or > PullMaxDistance)
            return;

        foreach (var direction in ApproachDirections(Player.Position, enemy.Position))
        {
            var target = Player.Position.Move(direction);

            if (!IsFree(target))
                continue;

            Player.Position = target;
            events.Add(new PlayerPulled(enemy.Id, target));
            return;
        }
    }
}
