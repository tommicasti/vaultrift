namespace Vaultrift.Infrastructure.Cosmos;

public interface IDocumentContainer
{
    T? Read<T>(string id, string partition) where T : class;

    bool Create<T>(T document, string partition) where T : class;

    void Upsert<T>(T document, string partition) where T : class;

    bool Delete(string id, string partition);

    bool Set(string id, string partition, IReadOnlyDictionary<string, object?> fields);

    long? Increment(string id, string partition, string field, long amount);

    IReadOnlyList<T> Query<T>(DocumentQuery query) where T : class;
}

public sealed record DocumentQuery(string Type, string Field, string Value, string? Partition = null, string? NewestFirstBy = null);
