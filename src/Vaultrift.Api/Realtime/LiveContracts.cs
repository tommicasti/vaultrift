using Vaultrift.Api.Contracts;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Api.Realtime;

public sealed record LiveWelcome(
    int HeroId,
    string ClassId,
    string HeroName,
    string? Biome,
    IReadOnlyList<string> Map,
    IReadOnlyList<EquippedItemResponse> Equipment,
    int TicksPerSecond,
    double MonsterWindupSeconds,
    double MonsterStrikeReach,
    LiveHeroState Hero,
    IReadOnlyList<LiveItemView> Items,
    string RoomCode,
    bool IsHost,
    int MaxHeroes,
    IReadOnlyList<LivePartyMember> Party);

public sealed record LivePartyMember(int HeroId, string Name, string ClassId, bool IsHost, IReadOnlyList<EquippedItemResponse> Equipment);

public sealed record LiveHeroState(
    PlayerResponse Player,
    IReadOnlyList<InventoryEntryResponse> Inventory,
    int InventoryCapacity,
    int Floor,
    int FloorCount,
    Difficulty Difficulty,
    int Deaths);

public sealed record LiveItemView(int Id, int X, int Y, ItemResponse Item);

public sealed record LiveSnapshot(
    long Tick,
    double Time,
    IReadOnlyList<LiveHeroView> Heroes,
    IReadOnlyList<LiveMonsterView> Monsters,
    IReadOnlyList<LiveEventView> Events,
    IReadOnlyList<LiveProjectileView> Projectiles,
    LiveHeroState? Hero = null,
    IReadOnlyList<LiveItemView>? Items = null,
    IReadOnlyList<LivePartyMember>? Party = null);

public sealed record LiveHeroView(
    int Id,
    string Name,
    string ClassId,
    double X,
    double Y,
    double Facing,
    int Health,
    int MaxHealth,
    int Mana,
    int MaxMana,
    bool Moving,
    bool Dead,
    string Attack,
    double Reach,
    double ArcDegrees,
    bool Invisible,
    bool Bulwark,
    bool Fury);

public sealed record LiveMonsterView(
    int Id,
    string Kind,
    string Name,
    Rarity Rarity,
    int Level,
    bool IsBoss,
    double X,
    double Y,
    double Facing,
    double Radius,
    int Health,
    int MaxHealth,
    bool Awake,
    bool Moving,
    bool Burning,
    bool Frozen);

public sealed record LiveProjectileView(int Id, int OwnerId, string Kind, double X, double Y, double Facing, Element? Element);

public sealed record LiveEventView(
    string Type,
    int? HeroId = null,
    int? MonsterId = null,
    int? Amount = null,
    bool Critical = false,
    bool Missed = false,
    int? Experience = null,
    int? Levels = null,
    int? ItemId = null,
    string? ItemName = null,
    string? Ability = null,
    string? SpellId = null,
    Element? Element = null,
    double? X = null,
    double? Y = null,
    double? Radius = null);

public sealed record LiveActionResponse(IReadOnlyList<EventResponse> Events, LiveHeroState Hero);
