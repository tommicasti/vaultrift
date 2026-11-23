using Microsoft.Data.SqlClient;

namespace Vaultrift.Infrastructure.Persistence;

public sealed class SqlSaveRepository : ISaveRepository
{
    private const int MaxAttempts = 6;

    internal static readonly HashSet<int> TransientErrors =
        [-2, 20, 64, 233, 4060, 4221, 10053, 10054, 10060, 10928, 10929, 40143, 40197, 40501, 40613, 49918, 49919, 49920];

    private const string CreateTable = """
        IF OBJECT_ID(N'dbo.SavedGames', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.SavedGames (
                Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                Code CHAR(9) NOT NULL,
                ClassId NVARCHAR(32) NOT NULL,
                HeroName NVARCHAR(32) NOT NULL,
                Floor INT NOT NULL,
                Level INT NOT NULL,
                Data NVARCHAR(MAX) NOT NULL,
                CreatedAt DATETIMEOFFSET NOT NULL,
                UpdatedAt DATETIMEOFFSET NOT NULL
            );
            CREATE UNIQUE INDEX IX_SavedGames_Code ON dbo.SavedGames (Code);
        END
        """;

    private const string UpsertSql = """
        UPDATE dbo.SavedGames
        SET HeroName = @HeroName, Floor = @Floor, Level = @Level, Data = @Data, UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        IF @@ROWCOUNT = 0
            INSERT INTO dbo.SavedGames (Id, Code, ClassId, HeroName, Floor, Level, Data, CreatedAt, UpdatedAt)
            VALUES (@Id, @Code, @ClassId, @HeroName, @Floor, @Level, @Data, @UpdatedAt, @UpdatedAt);
        """;

    private const string SelectColumns = "SELECT Id, Code, ClassId, HeroName, Floor, Level, Data, UpdatedAt FROM dbo.SavedGames";

    private readonly string _connectionString;
    private readonly Func<int, TimeSpan> _delay;
    private readonly object _schemaGate = new();
    private bool _schemaReady;

    public SqlSaveRepository(string connectionString, Func<int, TimeSpan>? delay = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        _delay = delay ?? (attempt => TimeSpan.FromSeconds(Math.Min(2 << attempt, 20)));
    }

    public void Upsert(SaveRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = UpsertSql;
            command.Parameters.AddWithValue("@Id", record.Id);
            command.Parameters.AddWithValue("@Code", record.Code);
            command.Parameters.AddWithValue("@ClassId", record.ClassId);
            command.Parameters.AddWithValue("@HeroName", record.HeroName);
            command.Parameters.AddWithValue("@Floor", record.Floor);
            command.Parameters.AddWithValue("@Level", record.Level);
            command.Parameters.AddWithValue("@Data", record.Data);
            command.Parameters.AddWithValue("@UpdatedAt", record.UpdatedAt);
            command.ExecuteNonQuery();
            return true;
        });
    }

    public SaveRecord? FindById(Guid id) =>
        Run(connection => QuerySingle(connection, $"{SelectColumns} WHERE Id = @Value", id));

    public SaveRecord? FindByCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return Run(connection => QuerySingle(connection, $"{SelectColumns} WHERE Code = @Value", code));
    }

    private static SaveRecord? QuerySingle(SqlConnection connection, string sql, object value)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Value", value);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new SaveRecord(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetString(6),
            reader.GetDateTimeOffset(7));
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
            catch (SqlException error) when (attempt < MaxAttempts && TransientErrors.Contains(error.Number))
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
