using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;
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

    [Fact]
    public void AGameWonBeforeTheRiftExisted_CanStillGoDownToIt()
    {
        var session = NewSession();
        var save = session.ToSave();
        var oldWin = save with
        {
            Game = save.Game with { FloorNumber = VaultFloors.RolledFloors, IsConquered = true, Stairs = null, BossDefeatAnnounced = true }
        };

        var resumed = GameSession.Resume(session.Id, session.Code, SaveFile.FromJson(oldWin.ToJson()));

        Assert.False(resumed.Game.IsConquered);
        Assert.Equal(save.Game.PlayerStart, resumed.Game.Stairs);
        Assert.Equal(VaultFloors.FinalFloor, resumed.Game.FloorCount);
    }

    [Theory]
    [InlineData(Difficulty.Hard)]
    [InlineData(Difficulty.Hardcore)]
    public void HarderGames_HaveStrongerMonsters_AndRemember(Difficulty difficulty)
    {
        var normal = GameSession.Start(Guid.NewGuid(), "ABCD-EFGH", ClassCatalog.Find("knight")!, null, 5);
        var hard = GameSession.Start(Guid.NewGuid(), "ABCD-EFGH", ClassCatalog.Find("knight")!, null, 5, difficulty);

        Assert.All(hard.Game.Enemies, enemy => Assert.Equal(1 + VaultFloors.HardEarlyLevelBonus, enemy.Level));
        Assert.All(normal.Game.Enemies, enemy => Assert.Equal(1, enemy.Level));

        var resumed = GameSession.Resume(hard.Id, hard.Code, SaveFile.FromJson(hard.ToSave().ToJson()));

        Assert.Equal(difficulty, resumed.Game.Difficulty);
        Assert.Equal(hard.ToSave().ToJson(), resumed.ToSave().ToJson());
    }

    [Fact]
    public void ASaveFromBeforeDifficulties_IsANormalGame()
    {
        var json = NewSession().ToSave().ToJson()
            .Replace(",\"Difficulty\":\"Normal\"", "")
            .Replace(",\"IsLost\":false", "");

        var resumed = GameSession.Resume(Guid.NewGuid(), "ABCD-EFGH", SaveFile.FromJson(json));

        Assert.Equal(Difficulty.Normal, resumed.Game.Difficulty);
        Assert.False(resumed.Game.IsLost);
    }

    [Fact]
    public void ASaveFromBeforeTheWaystones_GetsOneOnTheCurrentFloor()
    {
        var save = NewSession().ToSave();
        var old = save with { Game = save.Game with { DeepestFloor = null, ClearedFloors = null, Waystone = null, Totem = null, TotemUses = 0 } };

        var resumed = GameSession.Resume(Guid.NewGuid(), "ABCD-EFGH", SaveFile.FromJson(old.ToJson()));

        Assert.Equal(1, resumed.Game.DeepestFloor);
        Assert.Empty(resumed.Game.ClearedFloors);
        Assert.NotNull(resumed.Game.Waystone);
    }

    [Fact]
    public void TheSessionKnowsTheBiomeOfEveryFloor()
    {
        var session = NewSession();
        var save = session.ToSave();

        Assert.Equal(save.Biomes[0], session.BiomeOf(1));
        Assert.Equal(save.Biomes[^1], session.BiomeOf(VaultFloors.RolledFloors));
        Assert.Equal(RiftArena.BiomeId, session.BiomeOf(VaultFloors.FinalFloor));
        Assert.Null(session.BiomeOf(0));
    }

    [Fact]
    public void ActiveTurns_AddUpToThePlayTime_AndSurviveAReload()
    {
        var session = NewSession();
        var start = DateTimeOffset.UtcNow.AddMinutes(1);

        session.RecordPlay(start);
        session.RecordPlay(start.AddSeconds(40));
        session.RecordPlay(start.AddSeconds(100));

        var resumed = GameSession.Resume(session.Id, session.Code, SaveFile.FromJson(session.ToSave().ToJson()));

        Assert.Equal(session.PlaySeconds, resumed.PlaySeconds);
        Assert.True(resumed.PlaySeconds >= 100);
    }

    [Fact]
    public void LongPauses_DoNotCountAsPlayTime()
    {
        var session = NewSession();
        var start = DateTimeOffset.UtcNow.AddMinutes(1);

        session.RecordPlay(start);
        var before = session.PlaySeconds;
        session.RecordPlay(start.AddHours(5));
        session.RecordPlay(start.AddHours(5).AddSeconds(30));

        Assert.Equal(before + 30, session.PlaySeconds);
    }

    [Fact]
    public void SavesWithoutPlayTime_StillLoad()
    {
        var json = NewSession().ToSave().ToJson().Replace(",\"PlaySeconds\":0", "");
        var save = SaveFile.FromJson(json);

        Assert.Equal(0, save.PlaySeconds);
    }
}
