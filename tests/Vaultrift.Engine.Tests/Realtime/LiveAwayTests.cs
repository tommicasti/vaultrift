using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveAwayTests
{
    private static readonly string[] OpenRoom =
    [
        "###########",
        "#.........#",
        "#.........#",
        "#.........#",
        "###########"
    ];

    private static readonly EnemyKind Rat = new("rat", "Rat", new Stats(maxHealth: 10, attack: 6, defense: 0, critical: 0));

    private static readonly Stats HeroStats = new(maxHealth: 30, attack: 6, defense: 0, evasion: 0, critical: 0);

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static LiveFloor Floor(string[] rows, int startX = 1, int startY = 1) =>
        new(DungeonMap.Parse(rows), new Position(startX, startY), new NeverLucky());

    private static List<LiveEvent> Run(LiveFloor floor, double seconds)
    {
        var events = new List<LiveEvent>();

        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());

        return events;
    }

    [Fact]
    public void MonstersIgnoreAHeroWhoIsAway()
    {
        var floor = Floor(OpenRoom);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(2, 1), Rat));
        floor.SetAway(1, true);

        var events = Run(floor, 3);

        Assert.False(rat.IsAwake);
        Assert.Equal(hero.MaxHealth, hero.Health);
        Assert.DoesNotContain(events, e => e is HeroHit);
    }

    [Fact]
    public void AnAwakeMonsterLosesItsTargetWhenTheHeroGoesAway()
    {
        var floor = Floor(OpenRoom);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddMonster(new Enemy(10, new Position(2, 1), Rat));
        Run(floor, 0.2);

        floor.SetAway(1, true);
        var health = hero.Health;
        Run(floor, 3);

        Assert.Equal(health, hero.Health);
    }

    [Fact]
    public void TerrainDoesNotHurtAHeroWhoIsAway()
    {
        var floor = Floor(["#######", "#L....#", "#######"]);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.SetAway(1, true);

        Run(floor, 3);

        Assert.Equal(hero.MaxHealth, hero.Health);
    }

    [Fact]
    public void AHeroWhoIsAwayStandsStillAndIgnoresInput()
    {
        var floor = Floor(OpenRoom);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.SetInput(1, HeroInput.Of(1, 0, false));
        Run(floor, 0.2);

        floor.SetAway(1, true);
        var spot = hero.Position;
        floor.SetInput(1, HeroInput.Of(1, 0, false));
        Run(floor, 1);

        Assert.Equal(spot, hero.Position);
        Assert.False(hero.IsMoving);
    }

    [Fact]
    public void AHeroWhoComesBackCanMoveAndBeHurtAgain()
    {
        var floor = Floor(OpenRoom);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddMonster(new Enemy(10, new Position(2, 1), Rat));
        floor.SetAway(1, true);
        Run(floor, 1);

        floor.SetAway(1, false);
        Run(floor, 3);

        Assert.False(hero.IsAway);
        Assert.True(hero.Health < hero.MaxHealth);
    }
}
