using Vaultrift.Infrastructure.Cosmos;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Cosmos;

public sealed class CosmosSaveRepositoryTests
{
    private readonly MemoryDocumentContainer _games = new();
    private readonly CosmosSaveRepository _repository;

    public CosmosSaveRepositoryTests() => _repository = new CosmosSaveRepository(_games);

    [Fact]
    public void Upsert_StoresANewGameAsOneDocument()
    {
        var record = Record("ABCD-1234", floor: 1);

        _repository.Upsert(record);

        Assert.Equal(1, _games.Count);
        Assert.Equal(record, _repository.FindById(record.Id));
        Assert.Equal(record, _repository.FindByCode("ABCD-1234"));
    }

    [Fact]
    public void Upsert_UpdatesProgressButKeepsCodeClassAndCreationDate()
    {
        var first = Record("ABCD-1234", floor: 1);
        _repository.Upsert(first);
        var key = first.Id.ToString("D");
        var createdAt = _games.Raw(key, key)!["createdAt"]!.GetValue<string>();

        var later = first with { HeroName = "Renamed", Floor = 7, Level = 9, Data = "{\"floor\":7}", UpdatedAt = first.UpdatedAt.AddHours(2) };
        _repository.Upsert(later);

        var stored = _repository.FindById(first.Id);
        Assert.Equal(later, stored);
        Assert.Equal(createdAt, _games.Raw(key, key)!["createdAt"]!.GetValue<string>());
        Assert.Equal(1, _games.Count);
    }

    [Fact]
    public void Upsert_PatchesInsteadOfRewritingOnceTheGameExists()
    {
        var record = Record("ABCD-1234", floor: 1);
        _repository.Upsert(record);
        _games.Operations.Clear();

        _repository.Upsert(record with { Floor = 2 });

        Assert.Equal(["set"], _games.Operations);
    }

    [Fact]
    public void Upsert_CopiesTheReportFiguresOutOfTheSave()
    {
        var data = """{"Version":1,"RandomState":16561418138772153265,"Game":{"Deaths":3,"TurnNumber":812,"IsConquered":true,"Player":{"Coins":4520}},"PlaySeconds":5400}""";
        var record = Record("ABCD-1234", floor: 21) with { Data = data };

        _repository.Upsert(record);
        _repository.Upsert(record with { Data = data.Replace("\"Deaths\":3", "\"Deaths\":4", StringComparison.Ordinal) });

        var key = record.Id.ToString("D");
        var stats = _games.Raw(key, key)!["stats"]!;
        Assert.Equal(4, stats["deaths"]!.GetValue<int>());
        Assert.Equal(4520, stats["coins"]!.GetValue<long>());
        Assert.Equal(812, stats["turns"]!.GetValue<int>());
        Assert.True(stats["conquered"]!.GetValue<bool>());
        Assert.Equal(5400, stats["playSeconds"]!.GetValue<long>());
        Assert.Contains("16561418138772153265", _repository.FindById(record.Id)!.Data);
    }

    [Fact]
    public void Stats_AreZeroWhenTheSaveCannotBeRead()
    {
        Assert.Equal(SaveStats.Empty, SaveStats.From("not json"));
        Assert.Equal(SaveStats.Empty, SaveStats.From("{\"Game\":{}}"));
    }

    [Fact]
    public void Find_ReturnsNullForUnknownGames()
    {
        Assert.Null(_repository.FindById(Guid.NewGuid()));
        Assert.Null(_repository.FindByCode("ZZZZ-9999"));
    }

    [Fact]
    public void Dates_AreStoredInUtcSoTheyCompareAsText()
    {
        var record = Record("ABCD-1234", floor: 1) with { UpdatedAt = new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.FromHours(1)) };

        _repository.Upsert(record);

        var key = record.Id.ToString("D");
        Assert.Equal("2026-11-26T09:00:00+00:00", _games.Raw(key, key)!["updatedAt"]!.GetValue<string>());
        Assert.Equal(record.UpdatedAt, _repository.FindById(record.Id)!.UpdatedAt);
    }

    [Fact]
    public void Import_KeepsTheOriginalCreationDate()
    {
        var record = Record("ABCD-1234", floor: 3);
        var createdAt = record.UpdatedAt.AddDays(-5);

        new CosmosImport(_games, new MemoryDocumentContainer()).Game(record, createdAt);

        var key = record.Id.ToString("D");
        Assert.Equal(record, _repository.FindById(record.Id));
        Assert.Equal(createdAt, DateTimeOffset.Parse(_games.Raw(key, key)!["createdAt"]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
    }

    private static SaveRecord Record(string code, int floor) =>
        new(Guid.NewGuid(), code, "knight", "Casti", floor, floor + 2, "{\"floor\":" + floor + "}", new DateTimeOffset(2026, 11, 26, 8, 0, 0, TimeSpan.Zero));
}
