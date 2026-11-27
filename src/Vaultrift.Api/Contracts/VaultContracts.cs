using System.ComponentModel.DataAnnotations;
using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Api.Contracts;

public sealed class CreateHeroRequest
{
    public required string ClassId { get; init; }

    [StringLength(Player.MaxNameLength)]
    public string? Name { get; init; }
}

public sealed class CreateWorldRequest
{
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;

    public string? ClassId { get; init; }
}

public sealed record VaultResponse(IReadOnlyList<VaultHeroResponse> Heroes, IReadOnlyList<VaultWorldResponse> Worlds);

public sealed record VaultHeroResponse(
    Guid Id,
    string Name,
    string ClassId,
    int Level,
    IReadOnlyList<Difficulty> Played,
    long PlaySeconds,
    bool InWorld,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<HeroGearResponse> Gear);

public sealed record VaultWorldResponse(
    Guid Id,
    Difficulty Difficulty,
    int Floor,
    int DeepestFloor,
    int FloorCount,
    string? Biome,
    long PlaySeconds,
    bool IsOpen,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsConquered,
    IReadOnlyList<Guid> GraveHeroIds);

public sealed record DepositSlotResponse(int Index, ItemResponse Item, int Quantity);

public sealed record DepositResponse(
    int Capacity,
    IReadOnlyList<DepositSlotResponse> Items,
    IReadOnlyList<InventoryEntryResponse> Backpack,
    int BackpackCapacity,
    IReadOnlyList<EquippedItemResponse> Equipment);
