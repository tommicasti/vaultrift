using Microsoft.Data.SqlClient;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Cosmos;
using Vaultrift.Infrastructure.Persistence;

var sqlConnection = Environment.GetEnvironmentVariable("VAULTRIFT_SQL");
var cosmosConnection = Environment.GetEnvironmentVariable("VAULTRIFT_COSMOS");

if (string.IsNullOrWhiteSpace(sqlConnection) || string.IsNullOrWhiteSpace(cosmosConnection))
{
    Console.Error.WriteLine("Set VAULTRIFT_SQL (Azure SQL connection string) and VAULTRIFT_COSMOS (Cosmos DB connection string) before running.");
    return 1;
}

using var store = CosmosStore.Open(cosmosConnection, Environment.GetEnvironmentVariable("VAULTRIFT_COSMOS_DATABASE"));
var import = new CosmosImport(store.Games, store.Accounts);
var games = new CosmosSaveRepository(store.Games);
var accounts = new CosmosAccountRepository(store.Accounts);

using var connection = new SqlConnection(sqlConnection);
connection.Open();

var savedGames = Copy(connection, "SELECT Id, Code, ClassId, HeroName, Floor, Level, Data, CreatedAt, UpdatedAt FROM dbo.SavedGames", reader =>
{
    var record = new SaveRecord(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetString(6), reader.GetDateTimeOffset(8));
    import.Game(record, reader.GetDateTimeOffset(7));

    if (games.FindById(record.Id)?.Data != record.Data)
        throw new InvalidOperationException($"Game {record.Code} was not copied correctly.");
});

var copiedAccounts = Copy(connection, "SELECT Id, UserName, PasswordHash, Email, CreatedAt FROM dbo.Accounts", reader =>
{
    var account = new Account(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetDateTimeOffset(4));
    import.Account(account);

    if (accounts.FindByName(account.UserName)?.Id != account.Id)
        throw new InvalidOperationException($"Account {account.UserName} was not copied correctly.");
});

var links = Copy(connection, "SELECT AccountId, GameId, LinkedAt FROM dbo.AccountGames", reader =>
    import.Link(reader.GetGuid(0), reader.GetGuid(1), reader.GetDateTimeOffset(2)));

var trophies = Copy(connection, "SELECT AccountId, TrophyId, UnlockedAt FROM dbo.AccountTrophies", reader =>
    import.Trophy(reader.GetGuid(0), reader.GetString(1), reader.GetDateTimeOffset(2)));

var counters = Copy(connection, "SELECT AccountId, Name, Value FROM dbo.AccountCounters", reader =>
    import.Counter(reader.GetGuid(0), reader.GetString(1), reader.GetInt64(2)));

Console.WriteLine($"Copied {savedGames} games, {copiedAccounts} accounts, {links} game links, {trophies} trophies and {counters} counters.");
return 0;

static int Copy(SqlConnection connection, string query, Action<SqlDataReader> copy)
{
    using var command = connection.CreateCommand();
    command.CommandText = query;
    using var reader = command.ExecuteReader();

    var count = 0;
    while (reader.Read())
    {
        copy(reader);
        count++;

        if (count % 50 == 0)
            Console.WriteLine($"  {count}...");
    }

    return count;
}
