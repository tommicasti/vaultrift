using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Persistence;

public class WriteBehindSaveRepositoryTests
{
    private static SaveRecord Record(string code = "ABCD-EFGH", int floor = 1, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), code, "knight", "Ada", floor, 1, "{}", DateTimeOffset.UtcNow);

    private static void WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (!condition() && DateTime.UtcNow < deadline)
            Thread.Sleep(10);

        Assert.True(condition());
    }

    private sealed class FlakyRepository(int failures) : ISaveRepository
    {
        private readonly MemorySaveRepository _inner = new();
        private int _failuresLeft = failures;

        public int Attempts { get; private set; }

        public void Upsert(SaveRecord record)
        {
            Attempts++;

            if (_failuresLeft-- > 0)
                throw new InvalidOperationException("The database is asleep.");

            _inner.Upsert(record);
        }

        public SaveRecord? FindById(Guid id) => _inner.FindById(id);

        public SaveRecord? FindByCode(string code) => _inner.FindByCode(code);
    }

    [Fact]
    public void ASave_ReachesTheRealRepositoryInTheBackground()
    {
        var inner = new MemorySaveRepository();
        using var repository = new WriteBehindSaveRepository(inner);
        var record = Record();

        repository.Upsert(record);

        WaitFor(() => inner.FindById(record.Id) is not null);
        Assert.Equal(0, repository.PendingCount);
    }

    [Fact]
    public void APendingSave_CanAlreadyBeRead()
    {
        var inner = new FlakyRepository(failures: 100);
        using var repository = new WriteBehindSaveRepository(inner, retryDelay: TimeSpan.FromMinutes(1));
        var record = Record();

        repository.Upsert(record);

        Assert.Equal(record, repository.FindById(record.Id));
        Assert.Equal(record, repository.FindByCode(record.Code));
    }

    [Fact]
    public void AFailedSave_IsRetried_AndReported()
    {
        var inner = new FlakyRepository(failures: 2);
        var errors = 0;
        using var repository = new WriteBehindSaveRepository(inner, (_, _) => Interlocked.Increment(ref errors), TimeSpan.FromMilliseconds(20));
        var record = Record();

        repository.Upsert(record);

        WaitFor(() => inner.FindById(record.Id) is not null);
        Assert.Equal(2, errors);
    }

    [Fact]
    public void OnlyTheLatestVersionOfAGame_IsKept()
    {
        var inner = new MemorySaveRepository();
        var id = Guid.NewGuid();

        using (var repository = new WriteBehindSaveRepository(inner))
        {
            for (var floor = 1; floor <= 20; floor++)
                repository.Upsert(Record(floor: floor, id: id));
        }

        Assert.Equal(20, inner.FindById(id)!.Floor);
    }

    [Fact]
    public void Disposing_WritesEverythingStillPending()
    {
        var inner = new MemorySaveRepository();
        var repository = new WriteBehindSaveRepository(inner);
        var records = Enumerable.Range(0, 50).Select(index => Record(code: $"AAAA-{index:0000}".Replace('0', 'Z'))).ToList();

        records.ForEach(repository.Upsert);
        repository.Dispose();

        Assert.Equal(50, inner.Count);
    }
}
