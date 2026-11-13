using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Api.Contracts;

public sealed record GameStateResponse(
    Guid Id,
    IReadOnlyList<string> Map,
    PlayerResponse Player,
    IReadOnlyList<EnemyResponse> Enemies,
    IReadOnlyList<FloorItemResponse> FloorItems,
    IReadOnlyList<InventoryEntryResponse> Inventory,
    int InventoryCapacity,
    int Deaths);

public sealed record PlayerResponse(
    int X,
    int Y,
    Direction Facing,
    int Health,
    int MaxHealth,
    int Attack,
    int Defense,
    int Evasion,
    IReadOnlyList<EquippedItemResponse> Equipment,
    ItemResponse? QuickSlot,
    int QuickSlotQuantity,
    IReadOnlyList<ActiveBoostResponse> Boosts);

public sealed record EquippedItemResponse(EquipmentSlot Slot, int EntryId);

public sealed record ActiveBoostResponse(ItemResponse Item, ModifierResponse Modifier, int RemainingTurns);

public sealed record EnemyResponse(int Id, int X, int Y, int Health, int MaxHealth, bool IsAwake);

public sealed record ModifierResponse(StatType Stat, int Amount);

public sealed record ItemResponse(
    string Id,
    string Name,
    Rarity Rarity,
    string Category,
    string? Description,
    IReadOnlyList<ModifierResponse> Modifiers,
    int? Healing,
    ModifierResponse? Boost,
    int? BoostTurns);

public sealed record FloorItemResponse(int X, int Y, ItemResponse Item);

public sealed record InventoryEntryResponse(int Id, ItemResponse Item, int Quantity);

public sealed record TurnResponse(bool TurnPassed, IReadOnlyList<EventResponse> Events, GameStateResponse State);

public sealed record EventResponse(
    string Type,
    int? EnemyId = null,
    int? Damage = null,
    bool? IsCritical = null,
    bool? IsDodged = null,
    int? X = null,
    int? Y = null,
    ItemResponse? Item = null,
    string? Reason = null);