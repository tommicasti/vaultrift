using System.Text.Json;

namespace Vaultrift.Infrastructure.Persistence;

public sealed class FileSaveRepository : ISaveRepository
{
    private const string Extension = ".json";

    private readonly string _folder;
    private readonly object _gate = new();

    public FileSaveRepository(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        _folder = Path.GetFullPath(folder);
        Directory.CreateDirectory(_folder);
    }

    public void Upsert(SaveRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var path = PathFor(record.Code);
        var temporary = path + ".tmp";

        lock (_gate)
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(record));
            File.Move(temporary, path, overwrite: true);
        }
    }

    public SaveRecord? FindById(Guid id)
    {
        lock (_gate)
        {
            return Directory
                .EnumerateFiles(_folder, "*" + Extension)
                .Select(Read)
                .FirstOrDefault(record => record?.Id == id);
        }
    }

    public SaveRecord? FindByCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        lock (_gate)
        {
            var path = PathFor(code);
            return File.Exists(path) ? Read(path) : null;
        }
    }

    private string PathFor(string code)
    {
        var normalized = SaveCode.Normalize(code) ?? throw new ArgumentException($"Invalid save code '{code}'.", nameof(code));
        return Path.Combine(_folder, normalized + Extension);
    }

    private static SaveRecord? Read(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveRecord>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
