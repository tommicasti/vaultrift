using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Cosmos;

namespace Vaultrift.Infrastructure.Tests.Cosmos;

public sealed class CosmosAccountRepositoryTests
{
    private readonly MemoryDocumentContainer _documents = new();
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 11, 26, 8, 0, 0, TimeSpan.Zero));
    private readonly CosmosAccountRepository _repository;

    public CosmosAccountRepositoryTests() => _repository = new CosmosAccountRepository(_documents, _clock);

    [Fact]
    public void TryCreate_FindsTheAccountByIdAndByNameIgnoringCase()
    {
        var account = NewAccount("Tommaso");

        Assert.True(_repository.TryCreate(account));

        Assert.Equal(account, _repository.FindById(account.Id));
        Assert.Equal(account, _repository.FindByName("TOMMASO"));
        Assert.Equal(account, _repository.FindByName(" tommaso "));
        Assert.Null(_repository.FindByName("Luca"));
        Assert.Null(_repository.FindById(Guid.NewGuid()));
    }

    [Fact]
    public void All_ListsEveryAccountAndNothingElse()
    {
        var first = NewAccount("Tommaso");
        var second = NewAccount("Elisa");
        _repository.TryCreate(first);
        _repository.TryCreate(second);
        _repository.Link(first.Id, Guid.NewGuid());
        _repository.Unlock(first.Id, "first-blood", _clock.GetUtcNow());

        Assert.Equal(new[] { first.Id, second.Id }.Order(), _repository.All().Select(account => account.Id).Order());
    }

    [Fact]
    public void TryCreate_RejectsANameThatIsAlreadyTaken()
    {
        Assert.True(_repository.TryCreate(NewAccount("Tommaso")));

        var second = NewAccount("TOMMASO");

        Assert.False(_repository.TryCreate(second));
        Assert.Null(_repository.FindById(second.Id));
    }

    [Fact]
    public void TryCreate_ReleasesTheNameWhenTheAccountCannotBeWritten()
    {
        var account = NewAccount("Tommaso");
        Assert.True(_repository.TryCreate(account));

        Assert.False(_repository.TryCreate(account with { UserName = "Luca" }));

        Assert.Null(_repository.FindByName("Luca"));
        Assert.True(_repository.TryCreate(NewAccount("Luca")));
    }

    [Fact]
    public void Link_GivesEachGameOneOwner()
    {
        var tommaso = NewAccount("Tommaso");
        var luca = NewAccount("Luca");
        var game = Guid.NewGuid();

        Assert.Equal(LinkResult.Linked, _repository.Link(tommaso.Id, game));
        Assert.Equal(LinkResult.AlreadyYours, _repository.Link(tommaso.Id, game));
        Assert.Equal(LinkResult.OwnedByOther, _repository.Link(luca.Id, game));
        Assert.Equal(tommaso.Id, _repository.OwnerOf(game));
        Assert.Null(_repository.OwnerOf(Guid.NewGuid()));
    }

    [Fact]
    public void GamesOf_ListsTheNewestLinkFirst()
    {
        var account = NewAccount("Tommaso");
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();

        _repository.Link(account.Id, older);
        _clock.Advance(TimeSpan.FromMinutes(5));
        _repository.Link(account.Id, newer);
        _repository.Link(Guid.NewGuid(), someoneElse);

        Assert.Equal([newer, older], _repository.GamesOf(account.Id));
    }

    [Fact]
    public void Unlink_OnlyWorksForTheOwner()
    {
        var owner = Guid.NewGuid();
        var game = Guid.NewGuid();
        _repository.Link(owner, game);

        Assert.False(_repository.Unlink(Guid.NewGuid(), game));
        Assert.Equal(owner, _repository.OwnerOf(game));

        Assert.True(_repository.Unlink(owner, game));
        Assert.Null(_repository.OwnerOf(game));
        Assert.Empty(_repository.GamesOf(owner));
        Assert.False(_repository.Unlink(owner, game));
    }

    [Fact]
    public void Unlock_KeepsTheFirstUnlockDate()
    {
        var account = Guid.NewGuid();
        var first = new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero);

        Assert.True(_repository.Unlock(account, "first-blood", first));
        Assert.False(_repository.Unlock(account, "first-blood", first.AddDays(1)));
        Assert.True(_repository.Unlock(account, "rich", first.AddDays(2)));

        var trophies = _repository.TrophiesOf(account);
        Assert.Equal(2, trophies.Count);
        Assert.Equal(first, trophies["first-blood"]);
        Assert.Empty(_repository.TrophiesOf(Guid.NewGuid()));
    }

    [Fact]
    public void AddToCounter_AddsUpPerAccountAndName()
    {
        var account = Guid.NewGuid();

        Assert.Equal(40, _repository.AddToCounter(account, "coins", 40));
        Assert.Equal(55, _repository.AddToCounter(account, "coins", 15));
        Assert.Equal(1, _repository.AddToCounter(account, "conquered:knight", 1));
        Assert.Equal(3, _repository.AddToCounter(Guid.NewGuid(), "coins", 3));

        var counters = _repository.CountersOf(account);
        Assert.Equal(55, counters["coins"]);
        Assert.Equal(1, counters["conquered:knight"]);
        Assert.Equal(2, counters.Count);
    }

    [Fact]
    public void AddToCounter_IncrementsInPlaceOnceTheCounterExists()
    {
        var account = Guid.NewGuid();
        _repository.AddToCounter(account, "coins", 10);
        _documents.Operations.Clear();

        _repository.AddToCounter(account, "coins", 5);

        Assert.Equal(["increment"], _documents.Operations);
    }

    [Fact]
    public void Import_RecreatesAccountsLinksTrophiesAndCounters()
    {
        var account = NewAccount("Zeg");
        var game = Guid.NewGuid();
        var linkedAt = new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
        var import = new CosmosImport(new MemoryDocumentContainer(), _documents);

        import.Account(account);
        import.Account(account);
        import.Link(account.Id, game, linkedAt);
        import.Trophy(account.Id, "king-slayer", linkedAt);
        import.Counter(account.Id, "coins", 1200);

        Assert.Equal(account, _repository.FindByName("zeg"));
        Assert.Equal([game], _repository.GamesOf(account.Id));
        Assert.Equal(linkedAt, _repository.TrophiesOf(account.Id)["king-slayer"]);
        Assert.Equal(1250, _repository.AddToCounter(account.Id, "coins", 50));
        Assert.False(_repository.TryCreate(NewAccount("ZEG")));
    }

    private static Account NewAccount(string name) =>
        new(Guid.NewGuid(), name, "hash-" + name, name.ToLowerInvariant() + "@example.com", new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
