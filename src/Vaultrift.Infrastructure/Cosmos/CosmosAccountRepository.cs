using Vaultrift.Infrastructure.Accounts;

namespace Vaultrift.Infrastructure.Cosmos;

public sealed class CosmosAccountRepository : IAccountRepository
{
    private readonly IDocumentContainer _accounts;
    private readonly TimeProvider _clock;

    public CosmosAccountRepository(IDocumentContainer accounts, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        _accounts = accounts;
        _clock = clock ?? TimeProvider.System;
    }

    public bool TryCreate(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var name = NameOf(account);

        if (!_accounts.Create(name, name.Pk))
            return false;

        try
        {
            var document = AccountOf(account);
            if (_accounts.Create(document, document.Pk))
                return true;
        }
        catch
        {
            _accounts.Delete(name.Id, name.Pk);
            throw;
        }

        _accounts.Delete(name.Id, name.Pk);
        return false;
    }

    public Account? FindById(Guid id)
    {
        var key = DocumentKeys.Of(id);
        return ToAccount(_accounts.Read<AccountDocument>(key, key));
    }

    public Account? FindByName(string userName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var key = DocumentKeys.NameKey(AccountRules.NormalizeName(userName));
        var name = _accounts.Read<NameDocument>(key, key);
        return name is null ? null : ToAccount(_accounts.Read<AccountDocument>(name.AccountId, name.AccountId));
    }

    public LinkResult Link(Guid accountId, Guid gameId)
    {
        var owner = OwnerOf(accountId, gameId, _clock.GetUtcNow());

        if (_accounts.Create(owner, owner.Pk))
            return LinkResult.Linked;

        var existing = _accounts.Read<OwnerDocument>(owner.Id, owner.Pk);
        return existing?.AccountId == owner.AccountId ? LinkResult.AlreadyYours : LinkResult.OwnedByOther;
    }

    public bool Unlink(Guid accountId, Guid gameId)
    {
        var key = DocumentKeys.OwnerKey(gameId);
        var owner = _accounts.Read<OwnerDocument>(key, key);
        return owner?.AccountId == DocumentKeys.Of(accountId) && _accounts.Delete(key, key);
    }

    public IReadOnlyList<Guid> GamesOf(Guid accountId) =>
        _accounts
            .Query<OwnerDocument>(new DocumentQuery(DocumentTypes.Owner, "accountId", DocumentKeys.Of(accountId), NewestFirstBy: "linkedAt"))
            .Select(owner => Guid.Parse(owner.GameId))
            .ToList();

    public Guid? OwnerOf(Guid gameId)
    {
        var key = DocumentKeys.OwnerKey(gameId);
        var owner = _accounts.Read<OwnerDocument>(key, key);
        return owner is null ? null : Guid.Parse(owner.AccountId);
    }

    public bool Unlock(Guid accountId, string trophyId, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trophyId);

        var trophy = TrophyOf(accountId, trophyId, at);
        return _accounts.Create(trophy, trophy.Pk);
    }

    public IReadOnlyDictionary<string, DateTimeOffset> TrophiesOf(Guid accountId)
    {
        var key = DocumentKeys.Of(accountId);
        return _accounts
            .Query<TrophyDocument>(new DocumentQuery(DocumentTypes.Trophy, "accountId", key, key))
            .ToDictionary(trophy => trophy.TrophyId, trophy => trophy.UnlockedAt);
    }

    public long AddToCounter(Guid accountId, string name, long amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var counter = CounterOf(accountId, name, amount);

        for (var attempt = 0; ; attempt++)
        {
            if (_accounts.Increment(counter.Id, counter.Pk, "value", amount) is { } total)
                return total;

            if (_accounts.Create(counter, counter.Pk))
                return amount;

            if (attempt > 0)
                throw new InvalidOperationException($"Counter {name} of account {accountId} could not be updated.");
        }
    }

    public IReadOnlyDictionary<string, long> CountersOf(Guid accountId)
    {
        var key = DocumentKeys.Of(accountId);
        return _accounts
            .Query<CounterDocument>(new DocumentQuery(DocumentTypes.Counter, "accountId", key, key))
            .ToDictionary(counter => counter.Name, counter => counter.Value);
    }

    internal static NameDocument NameOf(Account account)
    {
        var key = DocumentKeys.NameKey(account.NormalizedName);
        return new NameDocument(key, key, DocumentTypes.Name, DocumentKeys.Of(account.Id));
    }

    internal static AccountDocument AccountOf(Account account)
    {
        var key = DocumentKeys.Of(account.Id);
        return new AccountDocument(key, key, DocumentTypes.Account, account.UserName, account.NormalizedName, account.PasswordHash, account.Email, account.CreatedAt.ToUniversalTime());
    }

    internal static OwnerDocument OwnerOf(Guid accountId, Guid gameId, DateTimeOffset linkedAt)
    {
        var key = DocumentKeys.OwnerKey(gameId);
        return new OwnerDocument(key, key, DocumentTypes.Owner, DocumentKeys.Of(gameId), DocumentKeys.Of(accountId), linkedAt.ToUniversalTime());
    }

    internal static TrophyDocument TrophyOf(Guid accountId, string trophyId, DateTimeOffset at) =>
        new(DocumentKeys.TrophyKey(trophyId), DocumentKeys.Of(accountId), DocumentTypes.Trophy, DocumentKeys.Of(accountId), trophyId, at.ToUniversalTime());

    internal static CounterDocument CounterOf(Guid accountId, string name, long value) =>
        new(DocumentKeys.CounterKey(name), DocumentKeys.Of(accountId), DocumentTypes.Counter, DocumentKeys.Of(accountId), name, value);

    private static Account? ToAccount(AccountDocument? document) =>
        document is null ? null : new Account(Guid.Parse(document.Id), document.UserName, document.PasswordHash, document.Email, document.CreatedAt);
}
