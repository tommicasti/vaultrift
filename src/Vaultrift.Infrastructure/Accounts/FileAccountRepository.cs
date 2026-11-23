using System.Text.Json;

namespace Vaultrift.Infrastructure.Accounts;

public sealed class FileAccountRepository : IAccountRepository
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly MemoryAccountRepository _memory = new();
    private readonly List<Account> _accounts = [];
    private readonly List<GameLink> _links = [];
    private readonly List<UnlockedTrophy> _trophies = [];
    private readonly Dictionary<string, Counter> _counters = [];

    public FileAccountRepository(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        Directory.CreateDirectory(folder);
        _path = Path.Combine(Path.GetFullPath(folder), "accounts.json");
        Load();
    }

    public bool TryCreate(Account account)
    {
        lock (_gate)
        {
            if (!_memory.TryCreate(account))
                return false;

            _accounts.Add(account);
            Write();
            return true;
        }
    }

    public Account? FindById(Guid id) => _memory.FindById(id);

    public Account? FindByName(string userName) => _memory.FindByName(userName);

    public LinkResult Link(Guid accountId, Guid gameId)
    {
        lock (_gate)
        {
            var result = _memory.Link(accountId, gameId);

            if (result == LinkResult.Linked)
            {
                _links.Add(new GameLink(accountId, gameId));
                Write();
            }

            return result;
        }
    }

    public bool Unlink(Guid accountId, Guid gameId)
    {
        lock (_gate)
        {
            if (!_memory.Unlink(accountId, gameId))
                return false;

            _links.RemoveAll(link => link.AccountId == accountId && link.GameId == gameId);
            Write();
            return true;
        }
    }

    public IReadOnlyList<Guid> GamesOf(Guid accountId) => _memory.GamesOf(accountId);

    public Guid? OwnerOf(Guid gameId) => _memory.OwnerOf(gameId);

    public bool Unlock(Guid accountId, string trophyId, DateTimeOffset at)
    {
        lock (_gate)
        {
            if (!_memory.Unlock(accountId, trophyId, at))
                return false;

            _trophies.Add(new UnlockedTrophy(accountId, trophyId, at));
            Write();
            return true;
        }
    }

    public IReadOnlyDictionary<string, DateTimeOffset> TrophiesOf(Guid accountId) => _memory.TrophiesOf(accountId);

    public long AddToCounter(Guid accountId, string name, long amount)
    {
        lock (_gate)
        {
            var total = _memory.AddToCounter(accountId, name, amount);
            _counters[$"{accountId}:{name}"] = new Counter(accountId, name, total);
            Write();
            return total;
        }
    }

    public IReadOnlyDictionary<string, long> CountersOf(Guid accountId) => _memory.CountersOf(accountId);

    private void Load()
    {
        if (!File.Exists(_path))
            return;

        var stored = JsonSerializer.Deserialize<StoredAccounts>(File.ReadAllText(_path));
        if (stored is null)
            return;

        foreach (var account in stored.Accounts)
        {
            if (_memory.TryCreate(account))
                _accounts.Add(account);
        }

        foreach (var link in stored.Links)
        {
            if (_memory.Link(link.AccountId, link.GameId) == LinkResult.Linked)
                _links.Add(link);
        }

        foreach (var trophy in stored.Trophies ?? [])
        {
            if (_memory.Unlock(trophy.AccountId, trophy.TrophyId, trophy.UnlockedAt))
                _trophies.Add(trophy);
        }

        foreach (var counter in stored.Counters ?? [])
        {
            _memory.AddToCounter(counter.AccountId, counter.Name, counter.Value);
            _counters[$"{counter.AccountId}:{counter.Name}"] = counter;
        }
    }

    private void Write()
    {
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new StoredAccounts(_accounts, _links, _trophies, [.. _counters.Values])));
        File.Move(temporary, _path, overwrite: true);
    }

    private sealed record GameLink(Guid AccountId, Guid GameId);

    private sealed record UnlockedTrophy(Guid AccountId, string TrophyId, DateTimeOffset UnlockedAt);

    private sealed record Counter(Guid AccountId, string Name, long Value);

    private sealed record StoredAccounts(List<Account> Accounts, List<GameLink> Links, List<UnlockedTrophy>? Trophies = null, List<Counter>? Counters = null);
}
