using Microsoft.Azure.Cosmos;

namespace Vaultrift.Infrastructure.Cosmos;

public sealed class CosmosStore : IDisposable
{
    public const string DefaultDatabase = "vaultrift";
    public const int SharedThroughput = 1000;
    public const string GamesContainer = "games";
    public const string AccountsContainer = "accounts";
    public const string PartitionPath = "/pk";

    private readonly CosmosClient _client;

    private CosmosStore(CosmosClient client, IDocumentContainer games, IDocumentContainer accounts)
    {
        _client = client;
        Games = games;
        Accounts = accounts;
    }

    public IDocumentContainer Games { get; }

    public IDocumentContainer Accounts { get; }

    public static CosmosStore Open(string connectionString, string? databaseName = null, Action<string, double>? onCharge = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var client = new CosmosClient(connectionString, new CosmosClientOptions { UseSystemTextJsonSerializerWithOptions = CosmosJson.Options });

        try
        {
            var database = client
                .CreateDatabaseIfNotExistsAsync(databaseName ?? DefaultDatabase, ThroughputProperties.CreateManualThroughput(SharedThroughput))
                .GetAwaiter().GetResult().Database;

            var games = new ContainerProperties(GamesContainer, PartitionPath);
            games.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            games.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/data/?" });

            var accounts = new ContainerProperties(AccountsContainer, PartitionPath);
            accounts.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            accounts.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/passwordHash/?" });

            return new CosmosStore(
                client,
                new CosmosDocumentContainer(database.CreateContainerIfNotExistsAsync(games).GetAwaiter().GetResult().Container, onCharge),
                new CosmosDocumentContainer(database.CreateContainerIfNotExistsAsync(accounts).GetAwaiter().GetResult().Container, onCharge));
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public void Dispose() => _client.Dispose();
}
