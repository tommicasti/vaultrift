using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Persistence;

public class GameSessionTests
{
    private static readonly Direction[] Path = [Direction.Right, Direction.Down, Direction.Down, Direction.Left, Direction.Up, Direction.Right];

    private static GameSession NewSession(string classId = "knight", ulong seed = 42) =>
        GameSession.Start(Guid.NewGuid(), "ABCD-EFGH", ClassCatalog.Find(classId)!, "Tommaso", seed);

    private static void PlayTurns(GameSession session, int turns)
    {
        for (var turn = 0; turn < turns; turn++)
        {
            session.Game.MovePlayer(Path[turn % Path.Length]);

            if (turn % 3 == 0)
                session.Game.Attack();
        }
    }

    [Fact]
    public void TheSameSeed_BuildsTheSameDungeon()
    {
        Assert.Equal(NewSession().ToSave().ToJson(), NewSession().ToSave().ToJson());
    }

    [Fact]
    public void DifferentSeeds_BuildDifferentDungeons()
    {
        Assert.NotEqual(NewSession(seed: 1).ToSave().ToJson(), NewSession(seed: 2).ToSave().ToJson());
    }

    [Theory]
    [InlineData("knight")]
    [InlineData("rogue")]
    [InlineData("ranger")]
    [InlineData("mage")]
    public void AResumedSession_StartsExactlyWhereTheSaveLeftOff(string classId)
    {
        var session = NewSession(classId);
        PlayTurns(session, 40);

        var json = session.ToSave().ToJson();
        var resumed = GameSession.Resume(session.Id, session.Code, SaveFile.FromJson(json));

        Assert.Equal(json, resumed.ToSave().ToJson());
        Assert.Equal(session.Game.Player.Name, resumed.Game.Player.Name);
    }

    [Fact]
    public void AResumedSession_PlaysOnExactlyLikeTheOriginal()
    {
        var session = NewSession("rogue", seed: 7);
        PlayTurns(session, 25);

        var resumed = GameSession.Resume(session.Id, session.Code, SaveFile.FromJson(session.ToSave().ToJson()));

        PlayTurns(session, 200);
        PlayTurns(resumed, 200);

        Assert.Equal(session.ToSave().ToJson(), resumed.ToSave().ToJson());
    }

    [Fact]
    public void ASaveKeepsTheRolledBiomes()
    {
        var session = NewSession(seed: 99);
        var save = SaveFile.FromJson(session.ToSave().ToJson());

        Assert.Equal(20, save.Biomes.Count);
        Assert.Equal(session.Game.Biome, save.Biomes[0]);
    }

    [Fact]
    public void ASaveFromAnotherVersion_IsRejected()
    {
        var json = NewSession().ToSave().ToJson().Replace("\"Version\":1", "\"Version\":99");

        Assert.Throws<InvalidDataException>(() => SaveFile.FromJson(json));
    }

    [Fact]
    public void TheRecord_DescribesTheHero()
    {
        var record = NewSession("mage").ToRecord();

        Assert.Equal("mage", record.ClassId);
        Assert.Equal("Tommaso", record.HeroName);
        Assert.Equal(1, record.Floor);
        Assert.Equal(1, record.Level);
    }
}
