using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Persistence;

public sealed class FileSaveRepositoryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "vaultrift-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static SaveRecord Record(string code, int floor = 1) =>
        new(Guid.NewGuid(), code, "knight", "Ada", floor, 1, "{\"data\":true}", DateTimeOffset.UtcNow);

    [Fact]
    public void ASavedRecord_CanBeFoundByCodeAndId()
    {
        var repository = new FileSaveRepository(_folder);
        var record = Record("ABCD-EFGH");

        repository.Upsert(record);

        Assert.Equal(record, repository.FindByCode("ABCD-EFGH"));
        Assert.Equal(record, repository.FindById(record.Id));
    }

    [Fact]
    public void SavingAgain_ReplacesTheRecord()
    {
        var repository = new FileSaveRepository(_folder);
        var record = Record("ABCD-EFGH");

        repository.Upsert(record);
        repository.Upsert(record with { Floor = 7 });

        Assert.Equal(7, repository.FindByCode("ABCD-EFGH")!.Floor);
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public void Saves_SurviveANewRepository()
    {
        var record = Record("K7M2-P9QX");
        new FileSaveRepository(_folder).Upsert(record);

        Assert.Equal(record, new FileSaveRepository(_folder).FindById(record.Id));
    }

    [Fact]
    public void MissingSaves_AreNotFound()
    {
        var repository = new FileSaveRepository(_folder);

        Assert.Null(repository.FindByCode("ZZZZ-ZZZZ"));
        Assert.Null(repository.FindById(Guid.NewGuid()));
    }

    [Fact]
    public void ACodeThatCouldEscapeTheFolder_IsRefused()
    {
        var repository = new FileSaveRepository(_folder);

        Assert.Throws<ArgumentException>(() => repository.FindByCode("../../secret"));
    }
}
