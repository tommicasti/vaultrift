using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Realtime;

namespace Vaultrift.Infrastructure.Tests.Realtime;

public sealed class LiveArenaTests
{
    private static (GameSession Session, LiveFloor Floor) Open(string classId, ulong seed = 7)
    {
        var session = GameSession.Start(Guid.NewGuid(), "LIVE", ClassCatalog.Find(classId)!, "Casti", seed);
        return (session, LiveArena.Open(session, new SeededRandom(seed)));
    }

    [Fact]
    public void TheHeroStartsAtTheEntranceOfTheFirstFloor()
    {
        var (session, floor) = Open("knight");
        var hero = Assert.Single(floor.Heroes);

        Assert.Equal(LiveArena.HeroId, hero.Id);
        Assert.Equal(session.Game.Player.Position.X, hero.Position.X);
        Assert.Equal(session.Game.Player.Position.Y, hero.Position.Y);
        Assert.Equal(session.Game.Player.CombatStats, hero.Stats);
        Assert.Equal(session.Game.Biome, floor.Biome);
    }

    [Fact]
    public void EveryMonsterThatCanMoveJoinsTheFloor()
    {
        var (session, floor) = Open("ranger");
        var movers = session.Game.Enemies.Where(enemy => !enemy.Kind.IsStationary).Select(enemy => enemy.Id).Order().ToList();

        Assert.Equal(movers, floor.Monsters.Select(monster => monster.Id).Order().ToList());
        Assert.DoesNotContain(floor.Monsters, monster => monster.Kind.IsStationary);
    }

    [Fact]
    public void FastClassesKeepTheirSpeed()
    {
        var (_, floor) = Open("rogue");

        Assert.Equal(Speed.Fast, floor.Heroes[0].Speed);
    }

    [Fact]
    public void TheFloorRunsWithoutErrors()
    {
        var (_, floor) = Open("berserker", seed: 99);
        floor.SetInput(LiveArena.HeroId, HeroInput.Of(0.6, 0.8, true));

        for (var tick = 0; tick < LiveRules.TicksPerSecond * 20; tick++)
            floor.Step();

        foreach (var monster in floor.Monsters)
            Assert.False(Collision.HitsWall(floor.Map, monster.Position, monster.Radius, monster.Kind.PassesThroughWalls));

        Assert.False(Collision.HitsWall(floor.Map, floor.Heroes[0].Position, LiveRules.HeroRadius));
    }
}
