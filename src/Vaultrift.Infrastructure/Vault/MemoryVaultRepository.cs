using System.Collections.Concurrent;

namespace Vaultrift.Infrastructure.Vault;

public sealed class MemoryVaultRepository : IVaultRepository
{
    private readonly ConcurrentDictionary<(Guid AccountId, VaultKind Kind, Guid Id), VaultEntry> _entries = new();

    public IReadOnlyList<VaultEntry> List(Guid accountId) =>
        _entries.Values.Where(entry => entry.AccountId == accountId).OrderByDescending(entry => entry.UpdatedAt).ToList();

    public VaultEntry? Find(Guid accountId, VaultKind kind, Guid id) => _entries.GetValueOrDefault((accountId, kind, id));

    public void Upsert(VaultEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries[(entry.AccountId, entry.Kind, entry.Id)] = entry;
    }

    public bool Delete(Guid accountId, VaultKind kind, Guid id) => _entries.TryRemove((accountId, kind, id), out _);
}
