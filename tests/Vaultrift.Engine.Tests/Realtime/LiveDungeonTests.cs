using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveDungeonTests
{
    private static readonly string[] Room =
    [
        "###########",
        "#.........#",
        "#.........#",
        "#.........#",
        "###########"
    ];

    private static readonly EnemyKind Rat = new("rat", "Rat", new Stats(maxHealth: 5, attack: 1, defense: 0, critical: 0));

    private static readonly EnemyKind Boss = new("ogre", "Ogre", new Stats(maxHealth: 5, attack: 1, defense: 0, critical: 0), isBoss: true);

    private static readonly Stats HeroStats = new(maxHealth: 30, attack: 50, defense: 0, critical: 0);

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static LiveFloor Floor(ILootTable? loot = null, long startTick = 0) =>
        new(DungeonMap.Parse(Room), new Position(1, 2), new NeverLucky(), loot: loot, floorNumber: 3, startTick: startTick);

    private static List<LiveEvent> Strike(LiveFloor floor, int heroId)
    {
        floor.SetInput(heroId, HeroInput.Of(0, 0, true));
        var events = floor.Step().ToList();
        floor.SetInput(heroId, HeroInput.Idle);
        return events;
    }

    private static List<LiveEvent> Walk(LiveFloor floor, int heroId, double moveX, double moveY, double seconds)
    {
        floor.SetInput(heroId, HeroInput.Of(moveX, moveY, false));
        var events = new List<LiveEvent>();

        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());

        floor.SetInput(heroId, HeroInput.Idle);
        return events;
    }

    [Fact]
    public void WalkingIntoAChestDoesNotOpenIt_StrikingItDoes()
    {
        var floor = Floor(new FixedLoot(new ItemInstance(TestItems.Sword)));
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddHero(2, "Elisa", "mage", HeroStats);
        floor.AddChest(new Position(3, 2));

        Assert.DoesNotContain(Walk(floor, 1, 1, 0, 1), e => e is ChestLooted);
        Assert.Single(floor.Chests);
        Assert.True(hero.Position.X < 3 - LiveRules.FixtureRadius);

        var events = Strike(floor, 1);

        var looted = Assert.Single(events.OfType<ChestLooted>());
        Assert.Equal(1, looted.HeroId);
        Assert.Equal(Progression.ChestCoins(3), looted.Coins);
        Assert.Equal(looted.Coins, hero.Player.Coins);
        Assert.Empty(floor.Chests);
        Assert.Equal(2, events.OfType<LootDropped>().Count());
    }

    [Fact]
    public void AMimicSpringsToLifeInsteadOfPaying()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var chest = floor.AddChest(new Position(3, 2), new Enemy(77, new Position(3, 2), Rat));

        Walk(floor, 1, 1, 0, 1);
        var events = Strike(floor, 1);

        var sprang = Assert.Single(events.OfType<MimicSprang>());
        Assert.Equal(chest.Id, sprang.ChestId);
        Assert.Equal(77, sprang.MonsterId);
        Assert.Contains(floor.Monsters, monster => monster.Id == 77 && monster.IsAwake);
        Assert.Equal(0, hero.Player.Coins);
        Assert.DoesNotContain(events, e => e is ChestLooted);
    }

    [Fact]
    public void StairsOpenOnlyOnceTheBossIsDown()
    {
        var floor = Floor();
        floor.AddHero(1, "Casti", "knight", HeroStats);
        var boss = floor.AddMonster(new Enemy(9, new Position(2, 2), Boss));
        floor.PlaceStairs(new Position(8, 2), leadsDeeper: true);

        Assert.False(floor.StairsOpen);

        floor.SetInput(1, HeroInput.Of(1, 0, true));
        var events = new List<LiveEvent>();
        for (var tick = 0; tick < LiveRules.TicksPerSecond && !boss.IsDead; tick++)
            events.AddRange(floor.Step());
        events.AddRange(floor.Step());

        Assert.True(boss.IsDead);
        Assert.True(floor.StairsOpen);
        Assert.Equal(new Position(8, 2), Assert.Single(events.OfType<StairsUnsealed>()).At);
    }

    [Fact]
    public void AHeroReachingOpenStairsIsAnnouncedOncePerVisit()
    {
        var floor = Floor();
        floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.PlaceStairs(new Position(3, 2), leadsDeeper: true);

        var arrivals = Walk(floor, 1, 1, 0, 0.6).Concat(Walk(floor, 1, 0, 0, 1)).OfType<HeroAtStairs>().ToList();
        Assert.Single(arrivals);

        var again = Walk(floor, 1, -1, 0, 0.8).Concat(Walk(floor, 1, 1, 0, 0.8)).OfType<HeroAtStairs>().ToList();
        Assert.Single(again);
    }

    [Fact]
    public void TheLastStairsNeverOpen()
    {
        var floor = Floor();
        floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.PlaceStairs(new Position(3, 2), leadsDeeper: false);

        var events = Walk(floor, 1, 1, 0, 1);

        Assert.False(floor.StairsOpen);
        Assert.DoesNotContain(events, e => e is HeroAtStairs or StairsUnsealed);
    }

    [Fact]
    public void ANewFloorKeepsTheClockRunning()
    {
        var floor = Floor(startTick: 400);

        floor.Step();

        Assert.Equal(401, floor.Tick);
        Assert.Equal(401 * LiveRules.TickSeconds, floor.Time, 6);
    }
}
