using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Infrastructure.Cosmos;

public sealed class CosmosVaultRepository : IVaultRepository
{
    private readonly IDocumentContainer _vault;

    public CosmosVaultRepository(IDocumentContainer vault)
    {
        ArgumentNullException.ThrowIfNull(vault);
        _vault = vault;
    }

    public IReadOnlyList<VaultEntry> List(Guid accountId)
    {
        var key = DocumentKeys.Of(accountId);
        return _vault
            .Query<VaultDocument>(new DocumentQuery(DocumentTypes.Vault, "accountId", key, key, "updatedAt"))
            .Select(ToEntry)
            .OfType<VaultEntry>()
            .ToList();
    }

    public VaultEntry? Find(Guid accountId, VaultKind kind, Guid id) =>
        ToEntry(_vault.Read<VaultDocument>(DocumentKeys.VaultKey(KindName(kind), id), DocumentKeys.Of(accountId)));

    public void Upsert(VaultEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var document = ToDocument(entry);
        _vault.Upsert(document, document.Pk);
    }

    public bool Delete(Guid accountId, VaultKind kind, Guid id) =>
        _vault.Delete(DocumentKeys.VaultKey(KindName(kind), id), DocumentKeys.Of(accountId));

    internal static VaultDocument ToDocument(VaultEntry entry)
    {
        var kind = KindName(entry.Kind);
        return new VaultDocument(
            DocumentKeys.VaultKey(kind, entry.Id),
            DocumentKeys.Of(entry.AccountId),
            DocumentTypes.Vault,
            DocumentKeys.Of(entry.AccountId),
            kind,
            DocumentKeys.Of(entry.Id),
            entry.Data,
            entry.CreatedAt.ToUniversalTime(),
            entry.UpdatedAt.ToUniversalTime());
    }

    private static string KindName(VaultKind kind) => kind.ToString().ToLowerInvariant();

    private static VaultEntry? ToEntry(VaultDocument? document)
    {
        if (document is null || !Enum.TryParse<VaultKind>(document.Kind, ignoreCase: true, out var kind))
            return null;

        return new VaultEntry(Guid.Parse(document.EntryId), Guid.Parse(document.AccountId), kind, document.Data, document.CreatedAt, document.UpdatedAt);
    }
}
