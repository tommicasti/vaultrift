using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Persistence;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;
using Vaultrift.Infrastructure.Realtime;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Infrastructure.Tests.Realtime;

public sealed class LiveArenaTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private static HeroSession Hero(string classId) =>
        HeroSession.Create(Guid.NewGuid(), Account, ClassCatalog.Find(classId)!, "Casti", Now);

    private static WorldSession World(Difficulty difficulty = Difficulty.Hard, ulong seed = 7, string classId = "knight") =>
        WorldSession.Start(Guid.NewGuid(), Account, classId, difficulty, Now, seed);

    private static (WorldSession World, LiveFloor Floor) OpenWorld(string classId = "knight", ulong seed = 7)
    {
        var world = World(seed: seed, classId: classId);
        return (world, LiveArena.Open(world));
    }

    [Fact]
    public void AWorldOpensOnItsCurrentFloor()
    {
        var (world, floor) = OpenWorld();

        Assert.Equal(1, floor.FloorNumber);
        Assert.Equal(world.BiomeOf(1), floor.Biome);
        Assert.Empty(floor.Heroes);
        Assert.NotEmpty(floor.Monsters);
    }

    [Fact]
    public void AResumedWorldOpensWhereThePartyStopped()
    {
        var world = World(seed: 13);
        world.Reach(4);

        var resumed = WorldSession.Restore(world.ToEntry(Now));
        var floor = LiveArena.Open(resumed);

        Assert.Equal(4, floor.FloorNumber);
        Assert.Equal(world.BiomeOf(4), floor.Biome);
    }

    [Fact]
    public void HeroesEnterAtTheEntranceOfTheWorld()
    {
        var (_, floor) = OpenWorld();
        var session = Hero("knight");

        var hero = LiveArena.Enter(floor, LiveArena.FirstHeroId, session);

        Assert.Same(session.Player, hero.Player);
        Assert.Equal(floor.Start, hero.Position);
        Assert.Equal(session.Player.CombatStats, hero.Stats);
    }

    [Fact]
    public void OnlyMonstersThatCanMoveJoinTheFloor()
    {
        var (_, floor) = OpenWorld("ranger");

        Assert.DoesNotContain(floor.Monsters, monster => monster.Kind.IsStationary);
    }

    [Fact]
    public void FloorsComeWithChestsAndStairsThatLeadDeeper()
    {
        var (world, _) = OpenWorld(seed: 21);

        var second = LiveArena.OpenFloor(world, 2, new SeededRandom(21), startTick: 900);

        Assert.Equal(2, second.FloorNumber);
        Assert.Equal(900, second.Tick);
        Assert.Equal(world.BiomeOf(2), second.Biome);
        Assert.NotEmpty(second.Chests);
        Assert.NotNull(second.Stairs);
        Assert.Equal(!second.Monsters.Any(monster => monster.Kind.IsBoss), second.StairsOpen);
    }

    [Fact]
    public void TheTwentiethFloorLeadsToTheRift()
    {
        var (world, _) = OpenWorld(seed: 5);

        Assert.True(LiveArena.LeadsDeeper(world, VaultFloors.RolledFloors));
        Assert.False(LiveArena.LeadsDeeper(world, VaultFloors.FinalFloor));
    }

    [Fact]
    public void TheRiftIsTheFinalFloorWithTheCustodeAndAWaystone()
    {
        var (world, _) = OpenWorld(seed: 5);

        var rift = LiveArena.OpenFloor(world, LiveArena.LastFloor, new SeededRandom(5));

        Assert.True(rift.IsFinal);
        Assert.Equal(RiftArena.BiomeId, rift.Biome);
        Assert.Null(rift.Stairs);
        Assert.Equal(RiftArena.Waystone, rift.Waystone);
        Assert.Contains(rift.Monsters, monster => monster.Kind.Id == RiftArena.GuardianId);
    }

    [Fact]
    public void AClearedFloorOffersATotemInsteadOfItsBoss()
    {
        var world = World(seed: 31);
        world.MarkCleared(2);

        var floor = LiveArena.OpenFloor(world, 2, world.Random);

        Assert.DoesNotContain(floor.Monsters, monster => monster.Kind.IsBoss);
        Assert.NotNull(floor.Totem);
        Assert.Equal(Game.TotemPriceFor(2, 0), floor.Totem!.Price);
        Assert.True(floor.StairsOpen);
    }

    [Fact]
    public void EveryFloorHasAWaystoneNextToTheEntrance()
    {
        var (_, floor) = OpenWorld(seed: 8);

        Assert.NotNull(floor.Waystone);
        Assert.Equal(1, Math.Abs(floor.Waystone!.Value.X - (int)floor.Start.X) + Math.Abs(floor.Waystone.Value.Y - (int)floor.Start.Y));
    }

    [Fact]
    public void FallsFollowTheDifficultyOfTheWorld()
    {
        Assert.Equal(FallRule.LoseCoins, LiveArena.OpenFloor(World(Difficulty.Normal), 1, new SeededRandom(1)).Falls);
        Assert.Equal(FallRule.DigGrave, LiveArena.OpenFloor(World(Difficulty.Hard), 1, new SeededRandom(1)).Falls);
        Assert.Equal(FallRule.Permadeath, LiveArena.OpenFloor(World(Difficulty.Hardcore), 1, new SeededRandom(1)).Falls);
    }

    [Fact]
    public void AGraveLeftInTheWorldWaitsNearTheEntrance()
    {
        var world = World(seed: 17);
        var hero = Hero("knight");
        var gear = hero.Player.Equipment.Select(pair => new GraveItemSnapshot(pair.Key, hero.Player.Capture().Equipment[pair.Key])).ToList();
        world.SetGraves(1, [new GraveSave(hero.Id, hero.Name, 1, gear)]);

        var resumed = WorldSession.Restore(world.ToEntry(Now));
        var floor = LiveArena.OpenFloor(resumed, 1, new SeededRandom(2));

        var grave = Assert.Single(floor.Graves);
        Assert.Equal(hero.Id, grave.Owner);
        Assert.Equal(gear.Count, grave.Count);
        Assert.True(Math.Abs(grave.Tile.X - (int)floor.Start.X) + Math.Abs(grave.Tile.Y - (int)floor.Start.Y) >= LiveArena.GraveDistance);
        Assert.Equal(gear.Count, LiveArena.GravesOf(floor)[0].Items.Count);
    }

    [Fact]
    public void FastClassesKeepTheirSpeed()
    {
        var (_, floor) = OpenWorld();

        var hero = LiveArena.Enter(floor, LiveArena.FirstHeroId, Hero("rogue"));

        Assert.Equal(Speed.Fast, hero.Speed);
    }

    [Fact]
    public void AnyWorldCanBePlayedAloneButOnlyHardWorldsAreShared()
    {
        Assert.Null(LiveArena.Refusal(World(Difficulty.Normal), shared: false));
        Assert.Null(LiveArena.Refusal(World(Difficulty.Hardcore), shared: false));
        Assert.Null(LiveArena.Refusal(World(Difficulty.Hard), shared: true));
        Assert.Equal(JoinRefusal.NotHard, LiveArena.Refusal(World(Difficulty.Normal), shared: true));
        Assert.Equal(JoinRefusal.Hardcore, LiveArena.Refusal(World(Difficulty.Hardcore), shared: true));
    }

    [Fact]
    public void TheFloorRunsWithoutErrors()
    {
        var (_, floor) = OpenWorld(seed: 99);
        LiveArena.Enter(floor, LiveArena.FirstHeroId, Hero("berserker"));
        floor.SetInput(LiveArena.FirstHeroId, HeroInput.Of(0.6, 0.8, true));

        for (var tick = 0; tick < LiveRules.TicksPerSecond * 20; tick++)
            floor.Step();

        foreach (var monster in floor.Monsters)
            Assert.False(Collision.HitsWall(floor.Map, monster.Position, monster.Radius, monster.Kind.PassesThroughWalls));

        Assert.False(Collision.HitsWall(floor.Map, floor.Heroes[0].Position, LiveRules.HeroRadius));
    }
}
