using System.Text.Json;

namespace Vaultrift.Infrastructure.Vault;

public sealed class FileVaultRepository : IVaultRepository
{
    private const string Extension = ".json";

    private readonly string _folder;
    private readonly Lock _gate = new();

    public FileVaultRepository(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        _folder = Path.Combine(Path.GetFullPath(folder), "vault");
        Directory.CreateDirectory(_folder);
    }

    public IReadOnlyList<VaultEntry> List(Guid accountId)
    {
        var folder = FolderOf(accountId);

        lock (_gate)
        {
            if (!Directory.Exists(folder))
                return [];

            return Directory
                .EnumerateFiles(folder, "*" + Extension)
                .Select(Read)
                .OfType<VaultEntry>()
                .OrderByDescending(entry => entry.UpdatedAt)
                .ToList();
        }
    }

    public VaultEntry? Find(Guid accountId, VaultKind kind, Guid id)
    {
        var path = PathOf(accountId, kind, id);

        lock (_gate)
            return File.Exists(path) ? Read(path) : null;
    }

    public void Upsert(VaultEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var path = PathOf(entry.AccountId, entry.Kind, entry.Id);
        var temporary = path + ".tmp";

        lock (_gate)
        {
            Directory.CreateDirectory(FolderOf(entry.AccountId));
            File.WriteAllText(temporary, JsonSerializer.Serialize(entry));
            File.Move(temporary, path, overwrite: true);
        }
    }

    public bool Delete(Guid accountId, VaultKind kind, Guid id)
    {
        var path = PathOf(accountId, kind, id);

        lock (_gate)
        {
            if (!File.Exists(path))
                return false;

            File.Delete(path);
            return true;
        }
    }

    private string FolderOf(Guid accountId) => Path.Combine(_folder, accountId.ToString("N"));

    private string PathOf(Guid accountId, VaultKind kind, Guid id) =>
        Path.Combine(FolderOf(accountId), $"{kind.ToString().ToLowerInvariant()}-{id:N}{Extension}");

    private static VaultEntry? Read(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<VaultEntry>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
