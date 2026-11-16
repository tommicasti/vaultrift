using Vaultrift.Engine.Combat;
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
    string? ClassId,
    int Level,
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
    IReadOnlyList<ActiveBoostResponse> Boosts,
    Element? Resistance);

public sealed record ClassResponse(
    string Id,
    string Name,
    string Description,
    int MaxHealth,
    int Attack,
    int Defense,
    int Evasion,
    WeaponType MasteryWeapon,
    ModifierResponse MasteryBonus);

public sealed record EquippedItemResponse(EquipmentSlot Slot, int EntryId);

public sealed record ActiveBoostResponse(ItemResponse Item, ModifierResponse Modifier, int RemainingTurns);

public sealed record EnemyResponse(
    int Id,
    string Kind,
    string Name,
    Rarity Rarity,
    int Level,
    int X,
    int Y,
    int Health,
    int MaxHealth,
    bool IsAwake,
    Element? Weakness,
    Element? Resistance,
    Element? AttackElement,
    bool IsBurning,
    bool IsFrozen);

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
    int? BoostTurns,
    Element? Element,
    int? EffectChance);

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
    string? Reason = null,
    Element? Element = null,
    Effectiveness? Effectiveness = null);