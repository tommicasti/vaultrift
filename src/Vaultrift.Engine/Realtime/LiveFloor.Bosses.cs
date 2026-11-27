using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed partial class LiveFloor
{
    private readonly Dictionary<Position, int> _coolingLava = [];
    private readonly HashSet<int> _fallenBosses = [];
    private bool _floorCleared;

    public bool IsFinal { get; init; }

    public bool IsCleared => _floorCleared;

    private static readonly Vector[] Neighbours =
        [new(-1, -1), new(0, -1), new(1, -1), new(-1, 0), new(1, 0), new(-1, 1), new(0, 1), new(1, 1)];

    private void WatchBosses(List<LiveEvent> events)
    {
        foreach (var boss in _monsters.Where(monster => monster.Kind.IsBoss && monster.IsDead && _fallenBosses.Add(monster.Id)).ToList())
        {
            events.Add(new BossSlain(boss.Id));

            foreach (var minion in _monsters.Where(monster => !monster.IsDead && monster.SummonerId == boss.Id))
            {
                minion.TakeDamage(minion.Health);
                events.Add(new MonsterWithered(minion.Id));
            }

            if (_floorCleared || _monsters.Any(monster => monster.Kind.IsBoss && !monster.IsDead))
                continue;

            _floorCleared = true;
            events.Add(new FloorCleared(FloorNumber));

            if (IsFinal)
                events.Add(new LiveVaultConquered());
        }

        AnnouncePhases(events);
    }

    private void AnnouncePhases(List<LiveEvent> events)
    {
        foreach (var boss in _monsters.Where(monster => monster.Kind.IsBoss && !monster.IsDead && monster.PhaseNumber > monster.AnnouncedPhase))
        {
            boss.AnnouncedPhase = boss.PhaseNumber;
            boss.PowerCharge = Game.PowerInterval - 1;
            events.Add(new LiveBossPhaseChanged(boss.Id, boss.PhaseNumber, boss.Power));
        }
    }

    private void UseBossPowers(List<LiveEvent> events)
    {
        foreach (var boss in _monsters.Where(monster => monster.Kind.IsBoss && !monster.IsDead && monster.IsAwake && monster.Power != BossPower.None).ToList())
        {
            if (TargetOf(boss) is not { } target)
                continue;

            if (++boss.PowerCharge < Game.PowerInterval)
                continue;

            boss.PowerCharge = 0;

            switch (boss.Power)
            {
                case BossPower.LavaRing:
                    SpreadLava(boss, events);
                    break;
                case BossPower.TentaclePull:
                    Pull(boss, target, events);
                    break;
                case BossPower.CrystalBloom:
                    BloomCrystals(boss, events);
                    break;
                case BossPower.SummonMinions:
                    Summon(boss, events);
                    break;
                case BossPower.Gust:
                    BlowAway(boss, target, events);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown boss power {boss.Power}.");
            }
        }
    }

    private LiveHero? TargetOf(LiveMonster monster) =>
        _heroes.FirstOrDefault(hero => hero.Id == monster.TargetId && !hero.IsDead && !hero.IsAway)
        ?? _heroes.Where(hero => hero.IsTargetable).OrderBy(hero => hero.Position.DistanceTo(monster.Position)).FirstOrDefault();

    private IEnumerable<Position> TilesAround(LiveMonster monster)
    {
        var center = Collision.TileOf(monster.Position);
        return Neighbours.Select(offset => new Position(center.X + (int)offset.X, center.Y + (int)offset.Y)).Where(Map.IsInside);
    }

    private bool HeroOn(Position tile) =>
        _heroes.Any(hero => !hero.IsDead && hero.Position.DistanceTo(new Vector(tile.X, tile.Y)) < 0.5 + LiveRules.HeroRadius);

    private bool ItemOn(Position tile) => _items.Any(item => item.Tile == tile) || _graves.Any(grave => grave.Tile == tile);

    private void ChangeTiles(IReadOnlyList<Position> tiles, TileType tile, int? monsterId, List<LiveEvent> events)
    {
        if (tiles.Count == 0)
            return;

        foreach (var position in tiles)
            Map.SetTile(position, tile);

        _fields.Clear();
        events.Add(new TilesChanged(tiles, tile, monsterId));
    }

    private void SpreadLava(LiveMonster boss, List<LiveEvent> events)
    {
        var tiles = TilesAround(boss).Where(tile => Map.GetTile(tile) == TileType.Floor && !IsFixture(tile)).ToList();

        foreach (var tile in tiles)
            _coolingLava[tile] = LiveRules.LavaCoolingSeconds;

        ChangeTiles(tiles, TileType.Lava, boss.Id, events);
    }

    private void CoolLava(List<LiveEvent> events)
    {
        var cooled = new List<Position>();

        foreach (var tile in _coolingLava.Keys.ToList())
        {
            if (--_coolingLava[tile] > 0)
                continue;

            _coolingLava.Remove(tile);
            cooled.Add(tile);
        }

        ChangeTiles(cooled, TileType.Floor, null, events);
    }

    private void BurnHeroesOnLava(List<LiveEvent> events)
    {
        foreach (var hero in _heroes.Where(hero => !hero.IsDead && Map.IsLava(Collision.TileOf(hero.Position)) && hero.Player.Resistance != Element.Fire).ToList())
        {
            var damage = Game.LavaDamage(hero.MaxHealth);
            events.Add(new HeroScorched(hero.Id, damage));
            Hurt(hero, damage, events);
        }
    }

    private void Pull(LiveMonster boss, LiveHero target, List<LiveEvent> events)
    {
        var offset = boss.Position - target.Position;
        var distance = offset.Length;

        if (distance < Game.PullMinDistance || distance > Game.PullMaxDistance)
            return;

        var from = target.Position;
        target.Position = Collision.Slide(Map, from, offset * (LiveRules.PullStep / distance), LiveRules.HeroRadius);

        if (target.Position == from)
            return;

        target.Touch();
        events.Add(new HeroPulled(target.Id, boss.Id));
    }

    private void BloomCrystals(LiveMonster boss, List<LiveEvent> events)
    {
        var around = TilesAround(boss).ToList();
        var crystals = around.Where(Map.IsCrystal).ToList();

        if (crystals.Count > 0)
        {
            ChangeTiles(crystals, TileType.Floor, boss.Id, events);

            foreach (var hero in _heroes.Where(hero => !hero.IsDead && crystals.Any(crystal => hero.Position.DistanceTo(new Vector(crystal.X, crystal.Y)) <= 1.5)).ToList())
            {
                var hit = DamageCalculator.BaseDamage(boss.Stats, hero.Stats);
                events.Add(new HeroShardHit(hero.Id, boss.Id, hit));
                Hurt(hero, hit, events);
            }

            return;
        }

        var tiles = around.Where(tile => Map.GetTile(tile) == TileType.Floor && !IsFixture(tile) && !HeroOn(tile) && !ItemOn(tile)).ToList();
        ChangeTiles(tiles, TileType.Crystal, boss.Id, events);
    }

    private void Summon(LiveMonster boss, List<LiveEvent> events)
    {
        if (boss.Kind.Minion is not { } minion)
            return;

        var tier = ReinforcementTier(_heroes.Count);
        var alive = _monsters.Count(monster => !monster.IsDead && monster.SummonerId == boss.Id);
        var count = Math.Min(Game.MinionsPerSummon + tier, Game.MaxMinions + LiveRules.HeroesPerReinforcement * tier - alive);
        var spots = TilesAround(boss)
            .Where(tile => Map.IsWalkable(tile) && !Map.IsLava(tile) && !HeroOn(tile) && !IsFixture(tile))
            .Take(Math.Max(0, count))
            .ToList();

        foreach (var spot in spots)
        {
            var summoned = AddMonster(new Enemy(_nextMonsterId++, spot, minion, Rarity.Common, boss.Level));
            summoned.SummonerId = boss.Id;
            summoned.IsAwake = true;
            summoned.AttackReadyAt = Time + LiveRules.MonsterWindupSeconds;
            events.Add(new MonsterSummoned(summoned.Id, boss.Id));
        }
    }

    private void BlowAway(LiveMonster boss, LiveHero target, List<LiveEvent> events)
    {
        var offset = target.Position - boss.Position;
        var distance = offset.Length;

        if (distance < 1e-6 || distance > LiveRules.GustRange + boss.Radius)
            return;

        var from = target.Position;
        target.Position = Collision.Slide(Map, from, offset * (LiveRules.GustDistance / distance), LiveRules.HeroRadius);
        var moved = target.Position.DistanceTo(from);
        var damage = moved < LiveRules.GustDistance * 0.9 ? Game.LavaDamage(target.MaxHealth) : 0;

        target.Touch();
        events.Add(new HeroBlownAway(target.Id, boss.Id, damage));
        Hurt(target, damage, events);
    }
}
