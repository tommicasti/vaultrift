using System.Text.Json;

namespace Vaultrift.Infrastructure.Cosmos;

public static class CosmosJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
