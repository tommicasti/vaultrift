using System.Text.Json;
using System.Text.Json.Serialization;
using Vaultrift.Engine.Persistence;

namespace Vaultrift.Infrastructure.Persistence;

public sealed record SaveFile(
    int Version,
    ulong RandomState,
    string ClassId,
    IReadOnlyList<string> Biomes,
    GameSnapshot Game)
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static SaveFile FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var save = JsonSerializer.Deserialize<SaveFile>(json, Options)
            ?? throw new InvalidDataException("The save file is empty.");

        if (save.Version != CurrentVersion)
            throw new InvalidDataException($"Unsupported save version {save.Version}.");

        return save;
    }
}
