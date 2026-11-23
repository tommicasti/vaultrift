namespace Vaultrift.Infrastructure.Accounts;

public sealed class MemoryAccountRepository : IAccountRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Account> _byName = [];
    private readonly Dictionary<Guid, Account> _byId = [];
    private readonly Dictionary<Guid, Guid> _owners = [];
    private readonly Dictionary<(Guid, string), DateTimeOffset> _trophies = [];
    private readonly Dictionary<(Guid, string), long> _counters = [];

    public bool TryCreate(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        lock (_gate)
        {
            if (!_byName.TryAdd(account.NormalizedName, account))
                return false;

            _byId[account.Id] = account;
            return true;
        }
    }

    public Account? FindById(Guid id)
    {
        lock (_gate)
            return _byId.GetValueOrDefault(id);
    }

    public Account? FindByName(string userName)
    {
        lock (_gate)
            return _byName.GetValueOrDefault(AccountRules.NormalizeName(userName));
    }

    public LinkResult Link(Guid accountId, Guid gameId)
    {
        lock (_gate)
        {
            if (_owners.TryGetValue(gameId, out var owner))
                return owner == accountId ? LinkResult.AlreadyYours : LinkResult.OwnedByOther;

            _owners[gameId] = accountId;
            return LinkResult.Linked;
        }
    }

    public bool Unlink(Guid accountId, Guid gameId)
    {
        lock (_gate)
            return _owners.TryGetValue(gameId, out var owner) && owner == accountId && _owners.Remove(gameId);
    }

    public IReadOnlyList<Guid> GamesOf(Guid accountId)
    {
        lock (_gate)
            return _owners.Where(pair => pair.Value == accountId).Select(pair => pair.Key).ToList();
    }

    public Guid? OwnerOf(Guid gameId)
    {
        lock (_gate)
            return _owners.TryGetValue(gameId, out var owner) ? owner : null;
    }

    public bool Unlock(Guid accountId, string trophyId, DateTimeOffset at)
    {
        lock (_gate)
            return _trophies.TryAdd((accountId, trophyId), at);
    }

    public IReadOnlyDictionary<string, DateTimeOffset> TrophiesOf(Guid accountId)
    {
        lock (_gate)
            return _trophies.Where(pair => pair.Key.Item1 == accountId).ToDictionary(pair => pair.Key.Item2, pair => pair.Value);
    }

    public long AddToCounter(Guid accountId, string name, long amount)
    {
        lock (_gate)
        {
            var total = _counters.GetValueOrDefault((accountId, name)) + amount;
            _counters[(accountId, name)] = total;
            return total;
        }
    }

    public IReadOnlyDictionary<string, long> CountersOf(Guid accountId)
    {
        lock (_gate)
            return _counters.Where(pair => pair.Key.Item1 == accountId).ToDictionary(pair => pair.Key.Item2, pair => pair.Value);
    }
}
