using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveBossTests
{
    private static readonly string[] Hall =
    [
        "###############",
        "#.............#",
        "#.............#",
        "#.............#",
        "#.............#",
        "#.............#",
        "###############"
    ];

    private static readonly Stats Sturdy = new(maxHealth: 400, attack: 1, defense: 50, critical: 0);

    private static readonly EnemyKind Imp = new("imp", "Imp", new Stats(maxHealth: 5, attack: 1, defense: 0, critical: 0));

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static EnemyKind Boss(BossPower power) =>
        new("boss", "Boss", new Stats(maxHealth: 200, attack: 3, defense: 0, critical: 0), isBoss: true, power: power, minion: Imp);

    private static (LiveFloor Floor, LiveHero Hero, LiveMonster Boss) Arena(BossPower power, int heroX = 4, bool final = false, bool rooted = false)
    {
        var floor = new LiveFloor(DungeonMap.Parse(Hall), new Position(heroX, 3), new NeverLucky(), floorNumber: 5) { IsFinal = final };
        var hero = floor.AddHero(1, "Casti", "knight", Sturdy);
        var kind = rooted
            ? new EnemyKind("rooted", "Rooted", new Stats(maxHealth: 200, attack: 3, defense: 0, critical: 0), isBoss: true, isStationary: true, power: power, minion: Imp)
            : Boss(power);
        var boss = floor.AddMonster(new Enemy(10, new Position(8, 3), kind));
        boss.IsAwake = true;
        return (floor, hero, boss);
    }

    private static List<LiveEvent> Wait(LiveFloor floor, double seconds)
    {
        var events = new List<LiveEvent>();

        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());

        return events;
    }

    [Fact]
    public void ALavaRingAppearsAroundTheBossAndCoolsDown()
    {
        var (floor, _, _) = Arena(BossPower.LavaRing);

        var events = Wait(floor, 3.1);

        var spread = events.OfType<TilesChanged>().First(change => change.Tile == TileType.Lava);
        Assert.NotEmpty(spread.Tiles);
        Assert.All(spread.Tiles, tile => Assert.True(floor.Map.IsLava(tile)));

        var later = Wait(floor, LiveRules.LavaCoolingSeconds + 0.1);
        Assert.Contains(later.OfType<TilesChanged>(), change => change.Tile == TileType.Floor && change.Tiles.Contains(spread.Tiles[0]));
    }

    [Fact]
    public void StandingOnLavaBurns()
    {
        var floor = new LiveFloor(DungeonMap.Parse(["#####", "#.L.#", "#####"]), new Position(2, 1), new NeverLucky());
        var hero = floor.AddHero(1, "Casti", "knight", Sturdy);

        var events = Wait(floor, 1.05);

        var burn = Assert.Single(events.OfType<HeroScorched>());
        Assert.Equal(Game.LavaDamage(Sturdy.MaxHealth), burn.Damage);
        Assert.Equal(Sturdy.MaxHealth - burn.Damage, hero.Health);
    }

    [Fact]
    public void TentaclesPullTheHeroCloser()
    {
        var (floor, hero, boss) = Arena(BossPower.TentaclePull, heroX: 4, rooted: true);
        var before = hero.Position.DistanceTo(boss.Position);

        var pulled = Wait(floor, 3.1);

        Assert.Single(pulled.OfType<HeroPulled>());
        Assert.Equal(before - LiveRules.PullStep, hero.Position.DistanceTo(boss.Position), 3);
    }

    [Fact]
    public void SummonersCallMinionsThatWitherWithThem()
    {
        var (floor, _, boss) = Arena(BossPower.SummonMinions);

        var events = Wait(floor, 3.1);
        var summoned = events.OfType<MonsterSummoned>().ToList();
        Assert.Equal(Game.MinionsPerSummon, summoned.Count);
        Assert.All(summoned, summon => Assert.Equal(boss.Id, floor.Monsters.Single(monster => monster.Id == summon.MonsterId).SummonerId));

        boss.TakeDamage(boss.Health);
        var after = Wait(floor, 0.1);

        Assert.Equal(summoned.Count, after.OfType<MonsterWithered>().Count());
        Assert.Contains(after, e => e is BossSlain);
    }

    [Fact]
    public void AGustThrowsTheHeroBack()
    {
        var (floor, hero, boss) = Arena(BossPower.Gust, heroX: 6);

        var events = Wait(floor, 3.1);

        var gust = Assert.Single(events.OfType<HeroBlownAway>());
        Assert.Equal(boss.Id, gust.MonsterId);
        Assert.True(hero.Position.X < 6);
    }

    [Fact]
    public void CrystalsGrowAroundTheBossAndThenBurst()
    {
        var (floor, _, _) = Arena(BossPower.CrystalBloom);

        var grown = Wait(floor, 3.1).OfType<TilesChanged>().Single(change => change.Tile == TileType.Crystal);
        Assert.All(grown.Tiles, tile => Assert.True(floor.Map.IsCrystal(tile)));

        var burst = Wait(floor, 3).OfType<TilesChanged>().Single(change => change.Tile == TileType.Floor);
        Assert.Equal(grown.Tiles.OrderBy(tile => tile.X).ThenBy(tile => tile.Y), burst.Tiles.OrderBy(tile => tile.X).ThenBy(tile => tile.Y));
    }

    [Fact]
    public void BossesAnnounceANewPhase()
    {
        var kind = new EnemyKind("custode", "Custode", new Stats(maxHealth: 100, attack: 3, defense: 0, critical: 0), isBoss: true, power: BossPower.LavaRing, minion: Imp)
        {
            Phases = [new BossPhase(100, BossPower.LavaRing, Speed.Normal), new BossPhase(50, BossPower.Gust, Speed.Fast)]
        };
        var floor = new LiveFloor(DungeonMap.Parse(Hall), new Position(2, 3), new NeverLucky());
        floor.AddHero(1, "Casti", "knight", Sturdy);
        var boss = floor.AddMonster(new Enemy(10, new Position(10, 3), kind));

        boss.TakeDamage(boss.MaxHealth / 2 + 1);
        var events = Wait(floor, 0.1);

        var phase = Assert.Single(events.OfType<LiveBossPhaseChanged>());
        Assert.Equal(2, phase.Phase);
        Assert.Equal(BossPower.Gust, phase.Power);
        Assert.Equal(Speed.Fast, boss.Speed);
    }

    [Fact]
    public void SlayingTheLastBossClearsTheFloorAndTheFinalOneConquersTheVault()
    {
        var (floor, _, boss) = Arena(BossPower.None, final: true);

        boss.TakeDamage(boss.Health);
        var events = Wait(floor, 0.1);

        Assert.Contains(events, e => e is FloorCleared { Floor: 5 });
        Assert.Single(events.OfType<LiveVaultConquered>());
        Assert.True(floor.IsCleared);
        Assert.Empty(Wait(floor, 1).OfType<LiveVaultConquered>());
    }
}
