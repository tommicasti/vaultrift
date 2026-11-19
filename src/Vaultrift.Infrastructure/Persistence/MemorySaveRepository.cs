using System.Collections.Concurrent;

namespace Vaultrift.Infrastructure.Persistence;

public sealed class MemorySaveRepository : ISaveRepository
{
    private readonly ConcurrentDictionary<Guid, SaveRecord> _records = new();

    public int Count => _records.Count;

    public void Upsert(SaveRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records[record.Id] = record;
    }

    public SaveRecord? FindById(Guid id) => _records.GetValueOrDefault(id);

    public SaveRecord? FindByCode(string code) =>
        _records.Values.FirstOrDefault(record => record.Code == code);
}
