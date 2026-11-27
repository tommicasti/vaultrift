using Microsoft.Data.SqlClient;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Vault;

public sealed class SqlVaultRepository : IVaultRepository
{
    private const int MaxAttempts = 6;

    private const string CreateTable = """
        IF OBJECT_ID(N'dbo.VaultEntries', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.VaultEntries (
                AccountId UNIQUEIDENTIFIER NOT NULL,
                Kind INT NOT NULL,
                Id UNIQUEIDENTIFIER NOT NULL,
                Data NVARCHAR(MAX) NOT NULL,
                CreatedAt DATETIMEOFFSET NOT NULL,
                UpdatedAt DATETIMEOFFSET NOT NULL,
                CONSTRAINT PK_VaultEntries PRIMARY KEY (AccountId, Kind, Id)
            );
        END
        """;

    private const string UpsertSql = """
        UPDATE dbo.VaultEntries SET Data = @Data, UpdatedAt = @UpdatedAt
        WHERE AccountId = @AccountId AND Kind = @Kind AND Id = @Id;
        IF @@ROWCOUNT = 0
            INSERT INTO dbo.VaultEntries (AccountId, Kind, Id, Data, CreatedAt, UpdatedAt)
            VALUES (@AccountId, @Kind, @Id, @Data, @CreatedAt, @UpdatedAt);
        """;

    private const string SelectColumns = "SELECT Id, AccountId, Kind, Data, CreatedAt, UpdatedAt FROM dbo.VaultEntries";

    private readonly string _connectionString;
    private readonly Func<int, TimeSpan> _delay;
    private readonly Lock _schemaGate = new();
    private bool _schemaReady;

    public SqlVaultRepository(string connectionString, Func<int, TimeSpan>? delay = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        _delay = delay ?? (attempt => TimeSpan.FromSeconds(Math.Min(2 << attempt, 20)));
    }

    public IReadOnlyList<VaultEntry> List(Guid accountId) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"{SelectColumns} WHERE AccountId = @AccountId ORDER BY UpdatedAt DESC";
            command.Parameters.AddWithValue("@AccountId", accountId);
            return ReadAll(command);
        });

    public VaultEntry? Find(Guid accountId, VaultKind kind, Guid id) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"{SelectColumns} WHERE AccountId = @AccountId AND Kind = @Kind AND Id = @Id";
            AddKey(command, accountId, kind, id);
            return ReadAll(command).FirstOrDefault();
        });

    public void Upsert(VaultEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = UpsertSql;
            AddKey(command, entry.AccountId, entry.Kind, entry.Id);
            command.Parameters.AddWithValue("@Data", entry.Data);
            command.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", entry.UpdatedAt);
            command.ExecuteNonQuery();
            return true;
        });
    }

    public bool Delete(Guid accountId, VaultKind kind, Guid id) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dbo.VaultEntries WHERE AccountId = @AccountId AND Kind = @Kind AND Id = @Id";
            AddKey(command, accountId, kind, id);
            return command.ExecuteNonQuery() > 0;
        });

    private static void AddKey(SqlCommand command, Guid accountId, VaultKind kind, Guid id)
    {
        command.Parameters.AddWithValue("@AccountId", accountId);
        command.Parameters.AddWithValue("@Kind", (int)kind);
        command.Parameters.AddWithValue("@Id", id);
    }

    private static List<VaultEntry> ReadAll(SqlCommand command)
    {
        using var reader = command.ExecuteReader();
        var entries = new List<VaultEntry>();

        while (reader.Read())
            entries.Add(new VaultEntry(reader.GetGuid(0), reader.GetGuid(1), (VaultKind)reader.GetInt32(2), reader.GetString(3), reader.GetDateTimeOffset(4), reader.GetDateTimeOffset(5)));

        return entries;
    }

    private T Run<T>(Func<SqlConnection, T> work)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                EnsureSchema(connection);
                return work(connection);
            }
            catch (SqlException error) when (attempt < MaxAttempts && SqlSaveRepository.TransientErrors.Contains(error.Number))
            {
                Thread.Sleep(_delay(attempt));
            }
        }
    }

    private void EnsureSchema(SqlConnection connection)
    {
        if (_schemaReady)
            return;

        lock (_schemaGate)
        {
            if (_schemaReady)
                return;

            using var command = connection.CreateCommand();
            command.CommandText = CreateTable;
            command.ExecuteNonQuery();
            _schemaReady = true;
        }
    }
}
