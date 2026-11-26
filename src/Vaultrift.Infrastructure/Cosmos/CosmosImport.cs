using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Cosmos;

public sealed class CosmosImport
{
    private readonly IDocumentContainer _games;
    private readonly IDocumentContainer _accounts;

    public CosmosImport(IDocumentContainer games, IDocumentContainer accounts)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentNullException.ThrowIfNull(accounts);
        _games = games;
        _accounts = accounts;
    }

    public void Game(SaveRecord record, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(record);
        var document = CosmosSaveRepository.ToDocument(record, createdAt);
        _games.Upsert(document, document.Pk);
    }

    public void Account(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        var name = CosmosAccountRepository.NameOf(account);
        var document = CosmosAccountRepository.AccountOf(account);
        _accounts.Upsert(document, document.Pk);
        _accounts.Upsert(name, name.Pk);
    }

    public void Link(Guid accountId, Guid gameId, DateTimeOffset linkedAt)
    {
        var owner = CosmosAccountRepository.OwnerOf(accountId, gameId, linkedAt);
        _accounts.Upsert(owner, owner.Pk);
    }

    public void Trophy(Guid accountId, string trophyId, DateTimeOffset unlockedAt)
    {
        var trophy = CosmosAccountRepository.TrophyOf(accountId, trophyId, unlockedAt);
        _accounts.Upsert(trophy, trophy.Pk);
    }

    public void Counter(Guid accountId, string name, long value)
    {
        var counter = CosmosAccountRepository.CounterOf(accountId, name, value);
        _accounts.Upsert(counter, counter.Pk);
    }
}
