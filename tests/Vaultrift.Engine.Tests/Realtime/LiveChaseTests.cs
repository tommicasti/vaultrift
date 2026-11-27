using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveChaseTests
{
    private static readonly string[] Corridor =
    [
        "########################################",
        "#......................................#",
        "#......................................#",
        "#......................................#",
        "########################################"
    ];

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static readonly Stats HeroStats = new(maxHealth: 40, attack: 1, defense: 0);

    private static LiveFloor Floor() => new(DungeonMap.Parse(Corridor), new Position(2, 2), new NeverLucky());

    private static LiveMonster Wolf(LiveFloor floor, int x) =>
        floor.AddMonster(new Enemy(9, new Position(x, 2), new Stats(maxHealth: 30, attack: 2, defense: 0)));

    private static void Run(LiveFloor floor, double seconds)
    {
        for (var tick = 0; tick < seconds * LiveRules.TicksPerSecond; tick++)
            floor.Step();
    }

    [Fact]
    public void AMonsterGivesUpOnAHeroThatGetsTooFarAndGoesBackToSleepAtHome()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var wolf = Wolf(floor, 6);
        hero.Position = new Vector(4, 2);

        Run(floor, 0.5);
        Assert.True(wolf.IsAwake);

        hero.Position = new Vector(4 + LiveRules.ChaseDistance + 12, 2);
        wolf.Position = new Vector(8, 2);
        Run(floor, 6);

        Assert.False(wolf.IsAwake);
        Assert.Equal(wolf.Home, wolf.Position);
    }

    [Fact]
    public void AMonsterStopsHuntingAHeroThatFell()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var wolf = Wolf(floor, 20);
        hero.Position = new Vector(17, 2);

        Run(floor, 0.5);
        Assert.True(wolf.IsAwake);

        hero.TakeDamage(1000);
        Run(floor, LiveRules.HeroRespawnSeconds + 6);

        Assert.False(hero.IsDead);
        Assert.False(wolf.IsAwake);
        Assert.Equal(wolf.Home, wolf.Position);
    }

    [Fact]
    public void AReturningMonsterTurnsBackWhenAHeroComesClose()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var wolf = Wolf(floor, 6);
        hero.Position = new Vector(4, 2);
        Run(floor, 0.5);

        hero.Position = new Vector(4 + LiveRules.ChaseDistance + 12, 2);
        Run(floor, 0.2);
        hero.Position = new Vector(wolf.Position.X + 2, 2);
        Run(floor, 0.5);

        Assert.True(wolf.IsAwake);
    }

    [Fact]
    public void AMonsterThatCannotFindItsWayHomeIsBackThereAfterAWhile()
    {
        var floor = new LiveFloor(DungeonMap.Parse(
        [
            "########################################",
            "#......#...............................#",
            "#......#...............................#",
            "#......................................#",
            "########################################"
        ]), new Position(2, 2), new NeverLucky());
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var wolf = Wolf(floor, 4);
        hero.Position = new Vector(5, 2);
        Run(floor, 0.5);

        wolf.Position = new Vector(9, 1);
        hero.Position = new Vector(38, 3);
        Run(floor, LiveRules.ReturnHomeSeconds + 1);

        Assert.False(wolf.IsAwake);
        Assert.Equal(wolf.Home, wolf.Position);
    }
}
