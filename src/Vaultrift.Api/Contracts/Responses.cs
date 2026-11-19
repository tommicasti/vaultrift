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
    int Deaths,
    int Floor,
    int FloorCount,
    StairsResponse? Stairs,
    bool IsConquered,
    string? Biome,
    IReadOnlyList<PositionResponse> Chests,
    MerchantResponse? Merchant);

public sealed record MerchantResponse(int X, int Y, bool IsNearby, IReadOnlyList<OfferResponse> Offers, int? UpgradePrice, int UpgradeSize);

public sealed record OfferResponse(int Id, ItemResponse Item, int Price);

public sealed record StairsResponse(int X, int Y, bool IsOpen);

public sealed record PlayerResponse(
    string? ClassId,
    string Name,
    int Level,
    int Experience,
    int ExperienceToNextLevel,
    int UnspentPoints,
    int Mana,
    int MaxMana,
    int Coins,
    int X,
    int Y,
    Direction Facing,
    int Health,
    int MaxHealth,
    int Attack,
    int Defense,
    int Evasion,
    int Critical,
    int Block,
    int FreeSteps,
    AbilityResponse? Ability,
    IReadOnlyList<EquippedItemResponse> Equipment,
    IReadOnlyList<QuickSlotResponse> QuickSlots,
    IReadOnlyList<SpellResponse?> Spells,
    int ActiveSpell,
    bool IsOverloaded,
    IReadOnlyList<ActiveBoostResponse> Boosts,
    Element? Resistance,
    int PoisonTurns);

public sealed record QuickSlotResponse(ItemResponse? Item, int Quantity);

public sealed record SpellResponse(string Id, string Name, Element Element, SpellShape Shape, int Power, int ManaCost, int Size, int ManaPercent);

public sealed record ClassResponse(
    string Id,
    string Name,
    string Description,
    int MaxHealth,
    int Attack,
    int Defense,
    int Evasion,
    int Critical,
    Speed Speed,
    WeaponType MasteryWeapon,
    ModifierResponse MasteryBonus,
    int MaxMana,
    ClassAbility? Ability,
    int AbilityCooldown);

public sealed record AbilityResponse(ClassAbility Kind, int Cooldown, int MaxCooldown, int ActiveTurns);

public sealed record EquippedItemResponse(EquipmentSlot Slot, int EntryId, ItemResponse Item, ItemMark Mark, int SellPrice);

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
    bool IsFrozen,
    bool IsBoss);
public sealed record ModifierResponse(StatType Stat, int Amount);

public sealed record EffectResponse(EffectKind Kind, int Amount);

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
    int? EffectChance,
    IReadOnlyList<EffectResponse> Effects,
    bool TwoHanded,
    string? ClassId,
    int? ManaRestore,
    SpellResponse? Spell);

public sealed record FloorItemResponse(int X, int Y, ItemResponse Item);

public sealed record InventoryEntryResponse(int Id, ItemResponse Item, int Quantity, ItemMark Mark, int SellPrice);

public sealed record TurnResponse(bool TurnPassed, IReadOnlyList<EventResponse> Events, GameStateResponse State);

public sealed record EventResponse(
    string Type,
    int? EnemyId = null,
    int? Damage = null,
    bool? IsCritical = null,
    bool? IsDodged = null,
    bool? IsBlocked = null,
    int? X = null,
    int? Y = null,
    ItemResponse? Item = null,
    string? Reason = null,
    Element? Element = null,
    Effectiveness? Effectiveness = null,
    IReadOnlyList<PositionResponse>? Tiles = null,
    int? Floor = null,
    int? Amount = null,
    int? Level = null,
    StatPoint? Stat = null,
    bool? Powered = null,
    ClassAbility? Ability = null,
    IReadOnlyList<PositionResponse>? Path = null,
    SpellResponse? Spell = null,
    int? Slot = null,
    int? Price = null,
    SpellResponse? Replaced = null);

public sealed record PositionResponse(int X, int Y);