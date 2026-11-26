using System.Text.Json;
using System.Text.Json.Nodes;
using Vaultrift.Infrastructure.Cosmos;

namespace Vaultrift.Infrastructure.Tests.Cosmos;

public sealed class MemoryDocumentContainer : IDocumentContainer
{
    private readonly object _gate = new();
    private readonly Dictionary<(string Partition, string Id), JsonObject> _documents = [];

    public int Count
    {
        get
        {
            lock (_gate)
                return _documents.Count;
        }
    }

    public List<string> Operations { get; } = [];

    public T? Read<T>(string id, string partition) where T : class
    {
        lock (_gate)
        {
            Operations.Add("read");
            return _documents.TryGetValue((partition, id), out var document) ? document.Deserialize<T>(CosmosJson.Options) : null;
        }
    }

    public bool Create<T>(T document, string partition) where T : class
    {
        var node = ToNode(document);

        lock (_gate)
        {
            Operations.Add("create");
            return _documents.TryAdd((partition, IdOf(node)), node);
        }
    }

    public void Upsert<T>(T document, string partition) where T : class
    {
        var node = ToNode(document);

        lock (_gate)
        {
            Operations.Add("upsert");
            _documents[(partition, IdOf(node))] = node;
        }
    }

    public bool Delete(string id, string partition)
    {
        lock (_gate)
        {
            Operations.Add("delete");
            return _documents.Remove((partition, id));
        }
    }

    public bool Set(string id, string partition, IReadOnlyDictionary<string, object?> fields)
    {
        lock (_gate)
        {
            Operations.Add("set");

            if (!_documents.TryGetValue((partition, id), out var document))
                return false;

            foreach (var field in fields)
                document[field.Key] = JsonSerializer.SerializeToNode(field.Value, CosmosJson.Options);

            return true;
        }
    }

    public long? Increment(string id, string partition, string field, long amount)
    {
        lock (_gate)
        {
            Operations.Add("increment");

            if (!_documents.TryGetValue((partition, id), out var document))
                return null;

            var total = (document[field]?.GetValue<long>() ?? 0) + amount;
            document[field] = total;
            return total;
        }
    }

    public IReadOnlyList<T> Query<T>(DocumentQuery query) where T : class
    {
        lock (_gate)
        {
            Operations.Add("query");

            var matches = _documents
                .Where(pair => query.Partition is null || pair.Key.Partition == query.Partition)
                .Select(pair => pair.Value)
                .Where(document => Text(document, "type") == query.Type && Text(document, query.Field) == query.Value);

            if (query.NewestFirstBy is { } order)
                matches = matches.OrderByDescending(document => Text(document, order), StringComparer.Ordinal);

            return matches.Select(document => document.Deserialize<T>(CosmosJson.Options)!).ToList();
        }
    }

    public JsonObject? Raw(string id, string partition)
    {
        lock (_gate)
            return _documents.TryGetValue((partition, id), out var document) ? (JsonObject)document.DeepClone() : null;
    }

    private static JsonObject ToNode<T>(T document) =>
        JsonSerializer.SerializeToNode(document, CosmosJson.Options)?.AsObject() ?? throw new ArgumentException("Documents must serialize to a JSON object.", nameof(document));

    private static string IdOf(JsonObject document) =>
        Text(document, "id") ?? throw new ArgumentException("Documents need an id.", nameof(document));

    private static string? Text(JsonObject document, string field) =>
        document[field] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
