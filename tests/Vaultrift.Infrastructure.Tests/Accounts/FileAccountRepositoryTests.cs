using Vaultrift.Infrastructure.Accounts;

namespace Vaultrift.Infrastructure.Tests.Accounts;

public sealed class FileAccountRepositoryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "vaultrift-accounts-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void AccountsAndLinks_SurviveARestart()
    {
        var account = new Account(Guid.NewGuid(), "Tommaso", PasswordHasher.Hash("password123", 1000), "me@example.com", DateTimeOffset.UtcNow);
        var game = Guid.NewGuid();
        var first = new FileAccountRepository(_folder);
        first.TryCreate(account);
        first.Link(account.Id, game);

        var reopened = new FileAccountRepository(_folder);

        Assert.Equal(account.Id, reopened.FindByName("TOMMASO")?.Id);
        Assert.Equal("me@example.com", reopened.FindById(account.Id)?.Email);
        Assert.Equal([game], reopened.GamesOf(account.Id));
        Assert.False(reopened.TryCreate(account with { Id = Guid.NewGuid() }));
    }
}
