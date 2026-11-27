namespace Vaultrift.Infrastructure.Vault;

public interface IVaultRepository
{
    IReadOnlyList<VaultEntry> List(Guid accountId);

    VaultEntry? Find(Guid accountId, VaultKind kind, Guid id);

    void Upsert(VaultEntry entry);

    bool Delete(Guid accountId, VaultKind kind, Guid id);
}
