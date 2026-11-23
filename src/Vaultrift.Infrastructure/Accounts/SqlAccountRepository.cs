using Microsoft.Data.SqlClient;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Accounts;

public sealed class SqlAccountRepository : IAccountRepository
{
    private const int MaxAttempts = 6;
    private const int DuplicateKey = 2627;
    private const int DuplicateIndex = 2601;

    private const string CreateTables = """
        IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.Accounts (
                Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                UserName NVARCHAR(20) NOT NULL,
                NormalizedName NVARCHAR(20) NOT NULL,
                PasswordHash NVARCHAR(200) NOT NULL,
                Email NVARCHAR(254) NULL,
                CreatedAt DATETIMEOFFSET NOT NULL
            );
            CREATE UNIQUE INDEX IX_Accounts_NormalizedName ON dbo.Accounts (NormalizedName);
        END;
        IF OBJECT_ID(N'dbo.AccountGames', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.AccountGames (
                GameId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                AccountId UNIQUEIDENTIFIER NOT NULL,
                LinkedAt DATETIMEOFFSET NOT NULL
            );
            CREATE INDEX IX_AccountGames_AccountId ON dbo.AccountGames (AccountId);
        END;
        IF OBJECT_ID(N'dbo.AccountTrophies', N'U') IS NULL
            CREATE TABLE dbo.AccountTrophies (
                AccountId UNIQUEIDENTIFIER NOT NULL,
                TrophyId NVARCHAR(40) NOT NULL,
                UnlockedAt DATETIMEOFFSET NOT NULL,
                CONSTRAINT PK_AccountTrophies PRIMARY KEY (AccountId, TrophyId)
            );
        IF OBJECT_ID(N'dbo.AccountCounters', N'U') IS NULL
            CREATE TABLE dbo.AccountCounters (
                AccountId UNIQUEIDENTIFIER NOT NULL,
                Name NVARCHAR(40) NOT NULL,
                Value BIGINT NOT NULL,
                CONSTRAINT PK_AccountCounters PRIMARY KEY (AccountId, Name)
            );
        """;

    private const string InsertTrophy = """
        INSERT INTO dbo.AccountTrophies (AccountId, TrophyId, UnlockedAt)
        SELECT @AccountId, @TrophyId, @UnlockedAt
        WHERE NOT EXISTS (SELECT 1 FROM dbo.AccountTrophies WITH (UPDLOCK, HOLDLOCK) WHERE AccountId = @AccountId AND TrophyId = @TrophyId);
        """;

    private const string AddCounter = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;
        UPDATE dbo.AccountCounters WITH (UPDLOCK, SERIALIZABLE)
        SET Value = Value + @Amount
        WHERE AccountId = @AccountId AND Name = @Name;
        IF @@ROWCOUNT = 0
            INSERT INTO dbo.AccountCounters (AccountId, Name, Value) VALUES (@AccountId, @Name, @Amount);
        COMMIT TRANSACTION;
        SELECT Value FROM dbo.AccountCounters WHERE AccountId = @AccountId AND Name = @Name;
        """;

    private const string InsertAccount = """
        INSERT INTO dbo.Accounts (Id, UserName, NormalizedName, PasswordHash, Email, CreatedAt)
        VALUES (@Id, @UserName, @NormalizedName, @PasswordHash, @Email, @CreatedAt);
        """;

    private const string SelectAccount = "SELECT Id, UserName, PasswordHash, Email, CreatedAt FROM dbo.Accounts";

    private const string InsertLink = """
        INSERT INTO dbo.AccountGames (GameId, AccountId, LinkedAt)
        SELECT @GameId, @AccountId, SYSDATETIMEOFFSET()
        WHERE NOT EXISTS (SELECT 1 FROM dbo.AccountGames WITH (UPDLOCK, HOLDLOCK) WHERE GameId = @GameId);
        """;

    private readonly string _connectionString;
    private readonly Func<int, TimeSpan> _delay;
    private readonly object _schemaGate = new();
    private bool _schemaReady;

    public SqlAccountRepository(string connectionString, Func<int, TimeSpan>? delay = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        _delay = delay ?? (attempt => TimeSpan.FromSeconds(Math.Min(2 << attempt, 20)));
    }

    public bool TryCreate(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = InsertAccount;
            command.Parameters.AddWithValue("@Id", account.Id);
            command.Parameters.AddWithValue("@UserName", account.UserName);
            command.Parameters.AddWithValue("@NormalizedName", account.NormalizedName);
            command.Parameters.AddWithValue("@PasswordHash", account.PasswordHash);
            command.Parameters.AddWithValue("@Email", (object?)account.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", account.CreatedAt);

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqlException error) when (error.Number is DuplicateKey or DuplicateIndex)
            {
                return false;
            }
        });
    }

    public Account? FindById(Guid id) =>
        Run(connection => QueryAccount(connection, $"{SelectAccount} WHERE Id = @Value", id));

    public Account? FindByName(string userName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        return Run(connection => QueryAccount(connection, $"{SelectAccount} WHERE NormalizedName = @Value", AccountRules.NormalizeName(userName)));
    }

    public LinkResult Link(Guid accountId, Guid gameId) =>
        Run(connection =>
        {
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = InsertLink;
                insert.Parameters.AddWithValue("@GameId", gameId);
                insert.Parameters.AddWithValue("@AccountId", accountId);

                try
                {
                    if (insert.ExecuteNonQuery() == 1)
                        return LinkResult.Linked;
                }
                catch (SqlException error) when (error.Number is DuplicateKey or DuplicateIndex)
                {
                }
            }

            using var owner = connection.CreateCommand();
            owner.CommandText = "SELECT AccountId FROM dbo.AccountGames WHERE GameId = @GameId";
            owner.Parameters.AddWithValue("@GameId", gameId);
            using var reader = owner.ExecuteReader();

            return reader.Read() && reader.GetGuid(0) == accountId ? LinkResult.AlreadyYours : LinkResult.OwnedByOther;
        });

    public bool Unlink(Guid accountId, Guid gameId) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dbo.AccountGames WHERE GameId = @GameId AND AccountId = @AccountId";
            command.Parameters.AddWithValue("@GameId", gameId);
            command.Parameters.AddWithValue("@AccountId", accountId);
            return command.ExecuteNonQuery() == 1;
        });

    public IReadOnlyList<Guid> GamesOf(Guid accountId) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT GameId FROM dbo.AccountGames WHERE AccountId = @AccountId ORDER BY LinkedAt DESC";
            command.Parameters.AddWithValue("@AccountId", accountId);
            using var reader = command.ExecuteReader();

            var games = new List<Guid>();
            while (reader.Read())
                games.Add(reader.GetGuid(0));

            return (IReadOnlyList<Guid>)games;
        });

    public Guid? OwnerOf(Guid gameId) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT AccountId FROM dbo.AccountGames WHERE GameId = @GameId";
            command.Parameters.AddWithValue("@GameId", gameId);
            using var reader = command.ExecuteReader();
            return reader.Read() ? reader.GetGuid(0) : (Guid?)null;
        });

    public bool Unlock(Guid accountId, string trophyId, DateTimeOffset at) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = InsertTrophy;
            command.Parameters.AddWithValue("@AccountId", accountId);
            command.Parameters.AddWithValue("@TrophyId", trophyId);
            command.Parameters.AddWithValue("@UnlockedAt", at);

            try
            {
                return command.ExecuteNonQuery() == 1;
            }
            catch (SqlException error) when (error.Number is DuplicateKey or DuplicateIndex)
            {
                return false;
            }
        });

    public IReadOnlyDictionary<string, DateTimeOffset> TrophiesOf(Guid accountId) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT TrophyId, UnlockedAt FROM dbo.AccountTrophies WHERE AccountId = @AccountId";
            command.Parameters.AddWithValue("@AccountId", accountId);
            using var reader = command.ExecuteReader();

            var trophies = new Dictionary<string, DateTimeOffset>();
            while (reader.Read())
                trophies[reader.GetString(0)] = reader.GetDateTimeOffset(1);

            return (IReadOnlyDictionary<string, DateTimeOffset>)trophies;
        });

    public long AddToCounter(Guid accountId, string name, long amount) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = AddCounter;
            command.Parameters.AddWithValue("@AccountId", accountId);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Amount", amount);
            using var reader = command.ExecuteReader();
            return reader.Read() ? reader.GetInt64(0) : amount;
        });

    public IReadOnlyDictionary<string, long> CountersOf(Guid accountId) =>
        Run(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name, Value FROM dbo.AccountCounters WHERE AccountId = @AccountId";
            command.Parameters.AddWithValue("@AccountId", accountId);
            using var reader = command.ExecuteReader();

            var counters = new Dictionary<string, long>();
            while (reader.Read())
                counters[reader.GetString(0)] = reader.GetInt64(1);

            return (IReadOnlyDictionary<string, long>)counters;
        });

    private static Account? QueryAccount(SqlConnection connection, string sql, object value)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Value", value);
        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Account(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetDateTimeOffset(4));
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
            command.CommandText = CreateTables;
            command.ExecuteNonQuery();
            _schemaReady = true;
        }
    }
}
