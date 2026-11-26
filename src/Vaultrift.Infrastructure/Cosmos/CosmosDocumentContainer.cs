using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Azure.Cosmos;

namespace Vaultrift.Infrastructure.Cosmos;

public sealed partial class CosmosDocumentContainer : IDocumentContainer
{
    private readonly Container _container;
    private readonly Action<string, double>? _onCharge;

    public CosmosDocumentContainer(Container container, Action<string, double>? onCharge = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        _container = container;
        _onCharge = onCharge;
    }

    public T? Read<T>(string id, string partition) where T : class
    {
        try
        {
            var response = Wait(_container.ReadItemAsync<T>(id, new PartitionKey(partition)));
            Charge("read", response.RequestCharge);
            return response.Resource;
        }
        catch (CosmosException error) when (error.StatusCode == HttpStatusCode.NotFound)
        {
            Charge("read", error.RequestCharge);
            return null;
        }
    }

    public bool Create<T>(T document, string partition) where T : class
    {
        try
        {
            Charge("create", Wait(_container.CreateItemAsync(document, new PartitionKey(partition))).RequestCharge);
            return true;
        }
        catch (CosmosException error) when (error.StatusCode == HttpStatusCode.Conflict)
        {
            Charge("create", error.RequestCharge);
            return false;
        }
    }

    public void Upsert<T>(T document, string partition) where T : class =>
        Charge("upsert", Wait(_container.UpsertItemAsync(document, new PartitionKey(partition))).RequestCharge);

    public bool Delete(string id, string partition)
    {
        try
        {
            Charge("delete", Wait(_container.DeleteItemAsync<object>(id, new PartitionKey(partition))).RequestCharge);
            return true;
        }
        catch (CosmosException error) when (error.StatusCode == HttpStatusCode.NotFound)
        {
            Charge("delete", error.RequestCharge);
            return false;
        }
    }

    public bool Set(string id, string partition, IReadOnlyDictionary<string, object?> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var operations = fields.Select(field => PatchOperation.Set("/" + FieldName(field.Key), field.Value)).ToList();

        try
        {
            Charge("set", Wait(_container.PatchItemAsync<object>(id, new PartitionKey(partition), operations)).RequestCharge);
            return true;
        }
        catch (CosmosException error) when (error.StatusCode == HttpStatusCode.NotFound)
        {
            Charge("set", error.RequestCharge);
            return false;
        }
    }

    public long? Increment(string id, string partition, string field, long amount)
    {
        var path = "/" + FieldName(field);

        try
        {
            var response = Wait(_container.PatchItemAsync<JsonElement>(id, new PartitionKey(partition), [PatchOperation.Increment(path, amount)]));
            Charge("increment", response.RequestCharge);
            return response.Resource.GetProperty(field).GetInt64();
        }
        catch (CosmosException error) when (error.StatusCode == HttpStatusCode.NotFound)
        {
            Charge("increment", error.RequestCharge);
            return null;
        }
    }

    public IReadOnlyList<T> Query<T>(DocumentQuery query) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);

        var text = $"SELECT * FROM c WHERE c.type = @type AND c.{FieldName(query.Field)} = @value";
        if (query.NewestFirstBy is { } order)
            text += $" ORDER BY c.{FieldName(order)} DESC";

        var definition = new QueryDefinition(text).WithParameter("@type", query.Type).WithParameter("@value", query.Value);
        var options = query.Partition is null ? null : new QueryRequestOptions { PartitionKey = new PartitionKey(query.Partition) };
        var results = new List<T>();

        using var iterator = _container.GetItemQueryIterator<T>(definition, requestOptions: options);
        while (iterator.HasMoreResults)
        {
            var page = Wait(iterator.ReadNextAsync());
            Charge("query", page.RequestCharge);
            results.AddRange(page);
        }

        return results;
    }

    private static string FieldName(string field) =>
        FieldPattern().IsMatch(field) ? field : throw new ArgumentException($"'{field}' is not a valid document field.", nameof(field));

    private static TResult Wait<TResult>(Task<TResult> task) => task.GetAwaiter().GetResult();

    private void Charge(string operation, double requestUnits) => _onCharge?.Invoke($"{_container.Id} {operation}", requestUnits);

    [GeneratedRegex("^[a-z][A-Za-z0-9]*$")]
    private static partial Regex FieldPattern();
}
