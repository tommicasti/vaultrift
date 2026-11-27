using System.Text.Json;
using Vaultrift.Engine.Persistence;

namespace Vaultrift.Infrastructure.Vault;

public sealed record DepositItem(ItemSnapshot Item, int Quantity);

public sealed record DepositSave(int Version, IReadOnlyList<DepositItem> Items)
{
    public const int CurrentVersion = 1;

    public static DepositSave Empty { get; } = new(CurrentVersion, []);

    public string ToJson() => JsonSerializer.Serialize(this, HeroSave.Options);

    public static DepositSave FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var save = JsonSerializer.Deserialize<DepositSave>(json, HeroSave.Options) ?? throw new InvalidDataException("The deposit is empty.");

        return save.Version == CurrentVersion ? save : throw new InvalidDataException($"Unsupported deposit version {save.Version}.");
    }
}

public enum DepositResult
{
    Done,
    HeroNotFound,
    ItemNotFound,
    DepositFull,
    BackpackFull
}
