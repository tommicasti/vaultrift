using System.Text.Json;
using System.Text.Json.Serialization;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Persistence;

namespace Vaultrift.Infrastructure.Vault;

public sealed record HeroSave(int Version, PlayerSnapshot Player, IReadOnlyList<Difficulty> Played, long PlaySeconds = 0, int Falls = 0)
{
    public const int CurrentVersion = 1;

    internal static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static HeroSave FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var save = JsonSerializer.Deserialize<HeroSave>(json, Options) ?? throw new InvalidDataException("The hero save is empty.");

        return save.Version == CurrentVersion ? save : throw new InvalidDataException($"Unsupported hero save version {save.Version}.");
    }
}

public sealed record WorldSave(
    int Version,
    ulong RandomState,
    string ClassId,
    Difficulty Difficulty,
    IReadOnlyList<string> Biomes,
    int Floor,
    int DeepestFloor,
    long PlaySeconds = 0,
    IReadOnlyList<int>? ClearedFloors = null,
    int TotemUses = 0,
    bool IsConquered = false,
    IReadOnlyList<GraveSave>? Graves = null)
{
    public const int CurrentVersion = 1;

    public string ToJson() => JsonSerializer.Serialize(this, HeroSave.Options);

    public static WorldSave FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var save = JsonSerializer.Deserialize<WorldSave>(json, HeroSave.Options) ?? throw new InvalidDataException("The world save is empty.");

        return save.Version == CurrentVersion ? save : throw new InvalidDataException($"Unsupported world save version {save.Version}.");
    }
}

public sealed record GraveSave(Guid Owner, string OwnerName, int Floor, IReadOnlyList<GraveItemSnapshot> Items);
