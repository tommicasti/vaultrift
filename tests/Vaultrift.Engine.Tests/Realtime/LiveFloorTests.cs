using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveFloorTests
{
    private static readonly string[] OpenRoom =
    [
        "###############",
        "#.............#",
        "#.............#",
        "#.............#",
        "#.............#",
        "#.............#",
        "###############"
    ];

    private static readonly string[] Corridor =
    [
        "##########",
        "#........#",
        "########.#",
        "#........#",
        "##########"
    ];

    private static readonly EnemyKind Rat = new("rat", "Rat", new Stats(maxHealth: 10, attack: 4, defense: 0, critical: 0));

    private static readonly Stats HeroStats = new(maxHealth: 30, attack: 6, defense: 0, critical: 0);

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static LiveFloor Floor(string[] rows, int startX = 1, int startY = 1) =>
        new(DungeonMap.Parse(rows), new Position(startX, startY), new NeverLucky());

    private static IReadOnlyList<LiveEvent> Run(LiveFloor floor, double seconds)
    {
        var events = new List<LiveEvent>();
        var ticks = (int)Math.Round(seconds * LiveRules.TicksPerSecond);

        for (var tick = 0; tick < ticks; tick++)
            events.AddRange(floor.Step());

        return events;
    }

    [Fact]
    public void HeroWalksAtTheirSpeed()
    {
        var floor = Floor(OpenRoom);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);

        floor.SetInput(1, HeroInput.Of(1, 0, false));
        Run(floor, 1);

        Assert.Equal(1 + LiveRules.HeroSpeed(Speed.Normal), hero.Position.X, 3);
        Assert.Equal(1, hero.Position.Y, 3);
        Assert.True(hero.IsMoving);
    }

    [Fact]
    public void FastHeroesWalkFaster()
    {
        var floor = Floor(OpenRoom);
        var rogue = floor.AddHero(1, "Shade", "rogue", HeroStats, Speed.Fast);

        floor.SetInput(1, HeroInput.Of(1, 0, false));
        Run(floor, 1);

        Assert.Equal(1 + LiveRules.HeroSpeed(Speed.Fast), rogue.Position.X, 3);
    }

    [Fact]
    public void DiagonalInputIsNotFaster()
    {
        var input = HeroInput.Of(1, 1, false);

        Assert.Equal(1, input.Move.Length, 6);
    }

    [Fact]
    public void WallsStopTheHeroButLetThemSlideAlong()
    {
        var floor = Floor(OpenRoom);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);

        floor.SetInput(1, HeroInput.Of(-1, 1, false));
        Run(floor, 1);

        Assert.True(hero.Position.X >= 1 - 0.5 + LiveRules.HeroRadius - 1e-6);
        Assert.True(hero.Position.Y > 2);
        Assert.False(Collision.HitsWall(floor.Map, hero.Position, LiveRules.HeroRadius));
    }

    [Fact]
    public void SwingHitsTheMonsterInFrontOnly()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var ahead = floor.AddMonster(new Enemy(10, new Position(6, 3), Rat));
        var behind = floor.AddMonster(new Enemy(11, new Position(4, 3), Rat));

        floor.SetInput(1, HeroInput.Of(1, 0, true));
        var events = floor.Step();

        Assert.Contains(events, e => e is HeroSwung { HeroId: 1 });
        Assert.Contains(events, e => e is MonsterHit { MonsterId: 10 });
        Assert.DoesNotContain(events, e => e is MonsterHit { MonsterId: 11 });
        Assert.True(ahead.Health < ahead.Stats.MaxHealth);
        Assert.Equal(behind.Stats.MaxHealth, behind.Health);
        Assert.True(hero.Facing.X > 0.99);
    }

    [Fact]
    public void HoldingAttackSwingsOnlyWhenTheWeaponIsReady()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        floor.AddHero(1, "Casti", "knight", HeroStats);

        floor.SetInput(1, HeroInput.Of(0, 0, true));
        var swings = Run(floor, 1).Count(e => e is HeroSwung);

        Assert.Equal((int)Math.Ceiling(1 / LiveRules.HeroAttackSeconds), swings);
    }

    [Fact]
    public void SlainMonstersAreAnnounced()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        floor.AddHero(1, "Casti", "knight", new Stats(maxHealth: 30, attack: 50, defense: 0, critical: 0));
        var rat = floor.AddMonster(new Enemy(10, new Position(6, 3), Rat));

        floor.SetInput(1, HeroInput.Of(1, 0, true));
        var events = floor.Step();

        Assert.True(rat.IsDead);
        Assert.Contains(events, e => e is MonsterSlain { HeroId: 1, MonsterId: 10 });
    }

    [Fact]
    public void MonstersSleepUntilTheHeroComesClose()
    {
        var floor = Floor(OpenRoom);
        floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(13, 5), Rat));

        Run(floor, 1);

        Assert.False(rat.IsAwake);
        Assert.Equal(rat.Home, rat.Position);
    }

    [Fact]
    public void AwakeMonstersChaseTheHero()
    {
        var floor = Floor(OpenRoom);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(5, 1), Rat));
        var start = rat.Position.DistanceTo(hero.Position);

        var events = Run(floor, 0.5);

        Assert.True(rat.IsAwake);
        Assert.Contains(events, e => e is MonsterWoke { MonsterId: 10 });
        Assert.True(rat.Position.DistanceTo(hero.Position) < start);
    }

    [Fact]
    public void MonstersFollowTheCorridorAroundWalls()
    {
        var floor = Floor(Corridor, startX: 1, startY: 3);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(7, 1), Rat));
        rat.IsAwake = true;

        Run(floor, 8);

        Assert.True(rat.Position.DistanceTo(hero.Position) < rat.Radius + LiveRules.HeroRadius + LiveRules.MonsterReach + 0.05);
        Assert.False(Collision.HitsWall(floor.Map, rat.Position, rat.Radius));
    }

    [Fact]
    public void MonstersGiveUpOnAHeroTooFarAlongThePath()
    {
        var floor = Floor(Corridor, startX: 1, startY: 3);
        floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(2, 1), Rat));
        rat.IsAwake = true;

        Run(floor, 2);

        Assert.False(rat.IsAwake);
        Assert.Equal(rat.Home, rat.Position);
    }

    [Fact]
    public void AMonsterHeadingHomeIgnoresHeroesUntilItGetsThere()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(9, 3), Rat));
        rat.IsAwake = true;
        rat.IsReturning = true;
        rat.Position = new Vector(6, 3);

        var events = Run(floor, 1);

        Assert.DoesNotContain(events, e => e is MonsterWindup or HeroHit);
        Assert.True(rat.Position.DistanceTo(rat.Home) < 3 - 0.5);
        Assert.Equal(hero.Stats.MaxHealth, hero.Health);
    }

    [Fact]
    public void ARisenHeroIsLeftAloneForAMoment()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(6, 3), Rat));
        hero.ShieldedUntil = floor.Time + LiveRules.RespawnShieldSeconds;

        Assert.DoesNotContain(Run(floor, LiveRules.RespawnShieldSeconds - 0.5), e => e is MonsterWoke or HeroHit);
        Assert.Contains(Run(floor, 1), e => e is MonsterWoke);
        Assert.True(rat.IsAwake);
    }

    [Fact]
    public void MonstersWindUpBeforeTheBlowLands()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddMonster(new Enemy(10, new Position(6, 3), Rat));

        var events = Run(floor, LiveRules.MonsterWindupSeconds * 2 + 0.2);
        var windup = events.ToList().FindIndex(e => e is MonsterWindup);
        var blow = events.ToList().FindIndex(e => e is HeroHit);

        Assert.True(windup >= 0);
        Assert.True(blow > windup);
        Assert.True(hero.Health < hero.Stats.MaxHealth);
    }

    [Fact]
    public void SteppingAwayDuringTheWindupDodgesTheBlow()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.AddMonster(new Enemy(10, new Position(6, 3), Rat));

        var events = new List<LiveEvent>();

        while (!events.Any(e => e is MonsterWindup))
            events.AddRange(floor.Step());

        floor.SetInput(1, HeroInput.Of(-1, 0, false));
        events.AddRange(Run(floor, LiveRules.MonsterWindupSeconds + 0.05));

        Assert.DoesNotContain(events, e => e is HeroHit);
        Assert.Equal(hero.Stats.MaxHealth, hero.Health);
    }

    [Fact]
    public void FallingCostsAQuarterOfTheCoins()
    {
        var floor = Floor(OpenRoom, startX: 2, startY: 2);
        var hero = floor.AddHero(1, "Casti", "knight", new Stats(maxHealth: 1, attack: 1, defense: 0, critical: 0));
        hero.Player.GainCoins(200);
        floor.AddMonster(new Enemy(10, new Position(3, 2), Rat));

        var events = new List<LiveEvent>();

        while (!hero.IsDead)
            events.AddRange(floor.Step());

        var fell = Assert.Single(events.OfType<HeroFell>());
        Assert.Equal(200 * Progression.CoinLossPercent / 100, fell.CoinsLost);
        Assert.Equal(200 - fell.CoinsLost, hero.Player.Coins);
    }

    [Fact]
    public void FallenHeroesRiseAtTheStartAfterAWhile()
    {
        var floor = Floor(OpenRoom, startX: 2, startY: 2);
        var hero = floor.AddHero(1, "Casti", "knight", new Stats(maxHealth: 1, attack: 1, defense: 0, critical: 0));
        floor.AddMonster(new Enemy(10, new Position(3, 2), Rat));

        var events = new List<LiveEvent>();

        while (!hero.IsDead)
            events.AddRange(floor.Step());

        Assert.Contains(events, e => e is HeroFell { HeroId: 1 });

        floor.SetInput(1, HeroInput.Of(1, 0, false));
        events.AddRange(Run(floor, LiveRules.HeroRespawnSeconds + 0.1));

        Assert.Contains(events, e => e is HeroRespawned { HeroId: 1 });
        Assert.Equal(hero.Stats.MaxHealth, hero.Health);
        Assert.Equal(floor.Start, hero.Position);
    }

    [Fact]
    public void AHeroWhoLeavesWhileFallenGoesHomeHealed()
    {
        var floor = Floor(OpenRoom, startX: 2, startY: 2);
        var hero = floor.AddHero(1, "Casti", "knight", new Stats(maxHealth: 1, attack: 1, defense: 0, critical: 0));
        floor.AddMonster(new Enemy(10, new Position(3, 2), Rat));

        while (!hero.IsDead)
            floor.Step();

        Assert.True(floor.RemoveHero(1));

        Assert.Equal(hero.Stats.MaxHealth, hero.Player.Health);
        Assert.Empty(floor.Heroes);
        Assert.False(floor.RemoveHero(1));
    }

    [Fact]
    public void MonstersWithNobodyToChaseWalkHomeAndHeal()
    {
        var floor = Floor(OpenRoom, startX: 2, startY: 2);
        var hero = floor.AddHero(1, "Casti", "knight", new Stats(maxHealth: 1, attack: 1, defense: 0, critical: 0));
        var rat = floor.AddMonster(new Enemy(10, new Position(6, 2), Rat));

        while (!hero.IsDead)
            floor.Step();

        Run(floor, 2);

        Assert.False(rat.IsAwake);
        Assert.Equal(rat.Home, rat.Position);
        Assert.Equal(rat.Stats.MaxHealth, rat.Health);
    }

    [Fact]
    public void HeroesCannotWalkThroughMonsters()
    {
        var floor = Floor(OpenRoom, startX: 5, startY: 3);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(7, 3), Rat));

        floor.SetInput(1, HeroInput.Of(1, 0, false));
        Run(floor, 1);

        Assert.True(hero.Position.DistanceTo(rat.Position) >= rat.Radius + LiveRules.HeroRadius - 1e-6);
    }

    [Fact]
    public void MonstersWithAClearViewWalkStraightAtTheHero()
    {
        var floor = Floor(OpenRoom, startX: 2, startY: 1);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        var rat = floor.AddMonster(new Enemy(10, new Position(6, 4), Rat));
        rat.IsAwake = true;
        var heading = (hero.Position - rat.Position).Normalized();
        var start = rat.Position;

        floor.Step();

        var moved = (rat.Position - start).Normalized();
        Assert.True(moved.Dot(heading) > 0.99);
    }

    [Fact]
    public void ClearPathSeesWalls()
    {
        var map = DungeonMap.Parse(Corridor);

        Assert.True(Collision.ClearPath(map, new Vector(1, 1), new Vector(8, 1), LiveRules.MonsterRadius));
        Assert.False(Collision.ClearPath(map, new Vector(1, 1), new Vector(1, 3), LiveRules.MonsterRadius));
    }

    [Fact]
    public void SprintingCoversMoreGround()
    {
        var walking = Floor(OpenRoom);
        var walker = walking.AddHero(1, "Casti", "knight", HeroStats);
        walking.SetInput(1, HeroInput.Of(1, 0, false));
        Run(walking, 1);

        var sprinting = Floor(OpenRoom);
        var sprinter = sprinting.AddHero(1, "Casti", "knight", HeroStats);
        sprinting.SetInput(1, HeroInput.Of(1, 0, false, sprint: true));
        Run(sprinting, 1);

        Assert.Equal((walker.Position.X - 1) * LiveRules.SprintFactor, sprinter.Position.X - 1, 2);
        Assert.True(sprinter.IsSprinting);
    }

    [Fact]
    public void ASprintingHeroCannotAttack()
    {
        var floor = Floor(OpenRoom);
        floor.AddHero(1, "Casti", "knight", HeroStats);
        floor.SetInput(1, HeroInput.Of(1, 0, true, sprint: true));

        var events = Run(floor, 1);

        Assert.DoesNotContain(events, e => e is HeroSwung);
    }

    [Fact]
    public void SprintingWakesMonstersFromFurtherAway()
    {
        var quiet = Floor(OpenRoom);
        quiet.AddHero(1, "Casti", "knight", HeroStats);
        var calm = quiet.AddMonster(new Enemy(10, new Position(11, 1), Rat));
        quiet.SetInput(1, HeroInput.Of(1, 0, false));
        Run(quiet, 0.5);

        var loud = Floor(OpenRoom);
        loud.AddHero(1, "Casti", "knight", HeroStats);
        var startled = loud.AddMonster(new Enemy(10, new Position(11, 1), Rat));
        loud.SetInput(1, HeroInput.Of(1, 0, false, sprint: true));
        Run(loud, 0.5);

        Assert.False(calm.IsAwake);
        Assert.True(startled.IsAwake);
    }

    [Fact]
    public void HeroesSlipAroundTheCornerOfADoorway()
    {
        var floor = Floor(["#####", "#...#", "##.##", "#...#", "#####"]);
        var hero = floor.AddHero(1, "Casti", "knight", HeroStats);
        hero.Position = new Vector(2.35, 1);
        floor.SetInput(1, HeroInput.Of(0, 1, false));

        Run(floor, 1);

        Assert.True(hero.Position.Y > 2);
    }
}
