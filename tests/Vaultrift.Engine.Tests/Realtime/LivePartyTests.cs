using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LivePartyTests
{
    private static readonly string[] Room =
    [
        "###########",
        "#.........#",
        "#.........#",
        "#.........#",
        "###########"
    ];

    private static readonly EnemyKind Rat = new("rat", "Rat", new Stats(maxHealth: 20, attack: 1, defense: 0, critical: 0));

    private static readonly Stats HeroStats = new(maxHealth: 30, attack: 3, defense: 0, critical: 0);

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static LiveFloor Floor() => new(DungeonMap.Parse(Room), new Position(5, 2), new NeverLucky());

    [Fact]
    public void AimingTurnsTheHeroEvenWhileWalkingElsewhere()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, "Casti", "ranger", HeroStats);

        floor.SetInput(1, HeroInput.Of(1, 0, false, 0, -1));
        floor.Step();

        Assert.True(hero.Position.X > 5);
        Assert.Equal(-1, hero.Facing.Y, 6);
    }

    [Fact]
    public void WithoutAimTheSwingTurnsToTheClosestMonsterInReach()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(5, 3), Rat));

        floor.SetInput(1, HeroInput.Of(0, 0, true));
        var events = floor.Step();

        Assert.Contains(events, e => e is MonsterHit { MonsterId: 10 });
        Assert.True(hero.Facing.Y > 0.99);
        Assert.True(rat.Health < rat.MaxHealth);
    }

    [Fact]
    public void AnExplicitAimIsNeverOverridden()
    {
        var floor = Floor();
        floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddMonster(new Enemy(10, new Position(5, 3), Rat));

        floor.SetInput(1, HeroInput.Of(0, 0, true, 0, -1));
        var events = floor.Step();

        Assert.DoesNotContain(events, e => e is MonsterHit);
    }

    [Fact]
    public void MonstersGrowTougherForEveryHeroWhoJoins()
    {
        var floor = Floor();
        var rat = floor.AddMonster(new Enemy(10, new Position(8, 1), Rat));
        floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddHero(2, "Elisa", "mage", HeroStats);
        floor.AddHero(3, "Luca", "rogue", HeroStats);

        Assert.Equal((int)Math.Round(20 * (1 + 2 * LiveRules.ExtraHeroHealth)), rat.MaxHealth);
        Assert.Equal(rat.MaxHealth, rat.Health);
    }

    [Fact]
    public void LeavingHeroesMakeMonstersWeakerAgainKeepingTheirWounds()
    {
        var floor = Floor();
        var rat = floor.AddMonster(new Enemy(10, new Position(8, 1), Rat));
        floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddHero(2, "Elisa", "mage", HeroStats);
        rat.TakeDamage(rat.MaxHealth / 2);

        Assert.True(floor.RemoveHero(2));

        Assert.Equal(20, rat.MaxHealth);
        Assert.Equal(10, rat.Health);
        Assert.Single(floor.Heroes);
    }

    [Fact]
    public void AFloorHoldsAtMostSixHeroes()
    {
        var floor = Floor();

        for (var id = 1; id <= LiveRules.MaxHeroes; id++)
            floor.AddHero(id, $"Hero{id}", "knight", HeroStats);

        Assert.Throws<InvalidOperationException>(() => floor.AddHero(99, "Late", "knight", HeroStats));
    }

    [Fact]
    public void MonstersHuntTheClosestHero()
    {
        var floor = Floor();
        floor.AddHero(1, "Casti", "knight", HeroStats);
        var near = floor.AddHero(2, "Elisa", "mage", HeroStats);
        near.Position = new Vector(8, 2);
        var rat = floor.AddMonster(new Enemy(10, new Position(9, 2), Rat));

        var events = new List<LiveEvent>();
        for (var tick = 0; tick < LiveRules.TicksPerSecond; tick++)
            events.AddRange(floor.Step());

        Assert.Contains(events, e => e is MonsterWindup { HeroId: 2 });
        Assert.Equal(2, rat.TargetId);
    }
}
