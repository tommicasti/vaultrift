using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Persistence;

public class CachedGameStoreTests
{
    private static ClassDefinition Knight => ClassCatalog.Find("knight")!;

    [Fact]
    public void Create_WritesTheNewGameAtOnce()
    {
        var repository = new MemorySaveRepository();
        var session = new CachedGameStore(repository).Create(Knight, "Ada");

        Assert.Equal(1, repository.Count);
        Assert.NotNull(repository.FindByCode(session.Code));
    }

    [Fact]
    public void Find_ReturnsTheCachedSession()
    {
        var store = new CachedGameStore(new MemorySaveRepository());
        var session = store.Create(Knight, null);

        Assert.Same(session, store.Find(session.Id));
    }

    [Fact]
    public void AfterARestart_TheGameIsLoadedFromTheSave()
    {
        var repository = new MemorySaveRepository();
        var first = new CachedGameStore(repository);
        var session = first.Create(Knight, "Ada");
        session.Game.MovePlayer(Direction.Right);
        first.Save(session);

        var restarted = new CachedGameStore(repository);
        var loaded = restarted.Find(session.Id);

        Assert.NotNull(loaded);
        Assert.NotSame(session, loaded);
        Assert.Equal(session.ToSave().ToJson(), loaded.ToSave().ToJson());
    }

    [Fact]
    public void FindByCode_AcceptsSloppyTyping()
    {
        var repository = new MemorySaveRepository();
        var session = new CachedGameStore(repository).Create(Knight, null);
        var typed = " " + session.Code.Replace("-", " ").ToLowerInvariant() + " ";

        var loaded = new CachedGameStore(repository).FindByCode(typed);

        Assert.Equal(session.Id, loaded?.Id);
    }

    [Fact]
    public void UnknownGames_AreNotFound()
    {
        var store = new CachedGameStore(new MemorySaveRepository());

        Assert.Null(store.Find(Guid.NewGuid()));
        Assert.Null(store.FindByCode("ZZZZ-ZZZZ"));
        Assert.Null(store.FindByCode("not a code"));
    }

    [Fact]
    public void Loading_TheSameGameTwice_GivesOneSession()
    {
        var repository = new MemorySaveRepository();
        var session = new CachedGameStore(repository).Create(Knight, null);
        var store = new CachedGameStore(repository);

        Assert.Same(store.Find(session.Id), store.FindByCode(session.Code));
        Assert.Equal(1, store.CachedCount);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    public void ASaveThisVersionCannotRead_IsReportedAsIncompatible(string data)
    {
        var repository = new MemorySaveRepository();
        repository.Upsert(new SaveRecord(Guid.NewGuid(), "ABCD-EFGH", "knight", "Ada", 1, 1, data, DateTimeOffset.UtcNow));

        var error = Assert.Throws<IncompatibleSaveException>(() => new CachedGameStore(repository).FindByCode("ABCD-EFGH"));

        Assert.Equal("ABCD-EFGH", error.Code);
    }

    [Fact]
    public void ASaveWithAnItemThatNoLongerExists_IsReportedAsIncompatible()
    {
        var repository = new MemorySaveRepository();
        var session = new CachedGameStore(repository).Create(Knight, null);
        var record = repository.FindById(session.Id)!;
        var broken = record with { Data = record.Data.Replace("potion-healing", "potion-that-was-removed") };
        repository.Upsert(broken);

        Assert.Throws<IncompatibleSaveException>(() => new CachedGameStore(repository).Find(session.Id));
    }
}
