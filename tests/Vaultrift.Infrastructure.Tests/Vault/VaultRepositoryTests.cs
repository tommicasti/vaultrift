using Vaultrift.Infrastructure.Cosmos;
using Vaultrift.Infrastructure.Tests.Cosmos;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Infrastructure.Tests.Vault;

public sealed class VaultRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "vault-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private IEnumerable<IVaultRepository> Repositories()
    {
        yield return new MemoryVaultRepository();
        yield return new FileVaultRepository(_folder);
        yield return new CosmosVaultRepository(new MemoryDocumentContainer());
    }

    private static VaultEntry Entry(Guid accountId, VaultKind kind, string data = "{}", int minutes = 0) =>
        new(Guid.NewGuid(), accountId, kind, data, Now, Now.AddMinutes(minutes));

    [Fact]
    public void EntriesAreKeptPerAccountNewestFirst()
    {
        foreach (var repository in Repositories())
        {
            var mine = Guid.NewGuid();
            var older = Entry(mine, VaultKind.Hero, minutes: 1);
            var newer = Entry(mine, VaultKind.World, minutes: 5);
            repository.Upsert(older);
            repository.Upsert(newer);
            repository.Upsert(Entry(Guid.NewGuid(), VaultKind.Hero));

            Assert.Equal([newer.Id, older.Id], repository.List(mine).Select(entry => entry.Id));
            Assert.Equal(older, repository.Find(mine, VaultKind.Hero, older.Id));
            Assert.Null(repository.Find(mine, VaultKind.World, older.Id));
            Assert.Null(repository.Find(Guid.NewGuid(), VaultKind.Hero, older.Id));
        }
    }

    [Fact]
    public void UpsertReplacesAndDeleteRemoves()
    {
        foreach (var repository in Repositories())
        {
            var account = Guid.NewGuid();
            var entry = Entry(account, VaultKind.Hero, "{\"a\":1}");
            repository.Upsert(entry);
            repository.Upsert(entry with { Data = "{\"a\":2}", UpdatedAt = Now.AddHours(1) });

            Assert.Equal("{\"a\":2}", repository.Find(account, VaultKind.Hero, entry.Id)!.Data);
            Assert.True(repository.Delete(account, VaultKind.Hero, entry.Id));
            Assert.False(repository.Delete(account, VaultKind.Hero, entry.Id));
            Assert.Empty(repository.List(account));
        }
    }

    [Fact]
    public void CosmosKeepsTheVaultInTheAccountPartition()
    {
        var container = new MemoryDocumentContainer();
        var account = Guid.NewGuid();
        var entry = Entry(account, VaultKind.World);

        new CosmosVaultRepository(container).Upsert(entry);

        var raw = container.Raw("world:" + entry.Id.ToString("D"), account.ToString("D"));
        Assert.NotNull(raw);
        Assert.Equal("vault", (string?)raw!["type"]);
    }

    [Fact]
    public void WriteBehindShowsPendingWritesAndDeletesRightAway()
    {
        var inner = new MemoryVaultRepository();
        var account = Guid.NewGuid();
        var kept = Entry(account, VaultKind.Hero);
        var gone = Entry(account, VaultKind.World);
        inner.Upsert(gone);

        using (var repository = new WriteBehindVaultRepository(inner))
        {
            repository.Upsert(kept);
            Assert.True(repository.Delete(account, VaultKind.World, gone.Id));

            Assert.Equal([kept.Id], repository.List(account).Select(entry => entry.Id));
            Assert.Null(repository.Find(account, VaultKind.World, gone.Id));
        }

        Assert.Equal([kept.Id], inner.List(account).Select(entry => entry.Id));
    }
}
