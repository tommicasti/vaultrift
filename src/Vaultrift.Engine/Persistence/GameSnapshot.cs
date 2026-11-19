using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Persistence;

public sealed record GameSnapshot(
    int FloorNumber,
    int Deaths,
    int TurnNumber,
    int StepsThisTurn,
    bool BossDefeatAnnounced,
    bool IsConquered,
    string? Biome,
    Position? Stairs,
    Position PlayerStart,
    IReadOnlyList<string> Map,
    PlayerSnapshot Player,
    IReadOnlyList<EnemySnapshot> Enemies,
    IReadOnlyList<FloorItemSnapshot> FloorItems,
    IReadOnlyList<ChestSnapshot> Chests,
    MerchantSnapshot? Merchant,
    IReadOnlyList<CoolingLavaSnapshot> CoolingLava);

public sealed record PlayerSnapshot(
    string? ClassId,
    string Name,
    StatsSnapshot BaseStats,
    int Level,
    int Experience,
    int UnspentPoints,
    IReadOnlyDictionary<StatPoint, int> SpentPoints,
    int Health,
    int Mana,
    int Coins,
    Position Position,
    Direction Facing,
    int AbilityCooldown,
    int BulwarkTurns,
    int FuryTurns,
    int InvisibleTurns,
    int PoisonTurns,
    bool IsOverloaded,
    int ActiveSpellIndex,
    InventorySnapshot Inventory,
    IReadOnlyDictionary<EquipmentSlot, EntrySnapshot> Equipment,
    IReadOnlyList<string?> QuickSlots,
    IReadOnlyList<string?> SpellSlots,
    IReadOnlyList<BoostSnapshot> Boosts);

public sealed record StatsSnapshot(int MaxHealth, int Attack, int Defense, int Evasion, int Critical, int Block, int MaxMana);

public sealed record InventorySnapshot(int Capacity, int NextEntryId, IReadOnlyList<EntrySnapshot> Entries);

public sealed record EntrySnapshot(int Id, ItemSnapshot Item, int Quantity, ItemMark Mark);

public sealed record ItemSnapshot(string Id, Element? Element);

public sealed record BoostSnapshot(string PotionId, int RemainingTurns);

public sealed record EnemySnapshot(
    int Id,
    string KindId,
    Rarity Rarity,
    int Level,
    Position Start,
    Position Position,
    int Health,
    bool IsAwake,
    bool IsResting,
    int BurnTurnsLeft,
    int BurnDamage,
    int FrozenTurnsLeft,
    int FreezeImmunityTurnsLeft,
    int PowerCharge,
    int? SummonerId);

public sealed record FloorItemSnapshot(Position Position, ItemSnapshot Item);

public sealed record ChestSnapshot(Position Position, EnemySnapshot? Mimic);

public sealed record MerchantSnapshot(Position Position, IReadOnlyList<OfferSnapshot> Offers);

public sealed record OfferSnapshot(int Id, ItemSnapshot Item, int Price);

public sealed record CoolingLavaSnapshot(Position Position, int TurnsLeft);
