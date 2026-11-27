using Vaultrift.Engine.Items;

namespace Vaultrift.Api.Contracts;

public sealed record PlayersReport(DateTimeOffset GeneratedAt, IReadOnlyList<PlayerReport> Players);

public sealed record PlayerReport(
    string UserName,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LastSeen,
    bool Online,
    long PlaySeconds,
    IReadOnlyList<ReportTrophy> Trophies,
    IReadOnlyList<ReportHero> Heroes,
    IReadOnlyList<ReportWorld> Worlds,
    ReportDeposit Deposit,
    IReadOnlyList<ReportTurnHero> TurnHeroes);

public sealed record ReportTrophy(string Name, string Description, DateTimeOffset UnlockedAt);

public sealed record ReportItem(string Name, Rarity Rarity, int Quantity, string? Slot = null);

public sealed record ReportHero(
    string Name,
    string ClassName,
    int Level,
    long Coins,
    long PlaySeconds,
    int Falls,
    bool Online,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ReportItem> Equipment,
    int BackpackItems);

public sealed record ReportWorld(
    string ClassName,
    string Difficulty,
    string? Biome,
    int Floor,
    int DeepestFloor,
    bool Conquered,
    bool Open,
    long PlaySeconds,
    DateTimeOffset UpdatedAt);

public sealed record ReportDeposit(int Items, IReadOnlyList<ReportItem> Treasures);

public sealed record ReportTurnHero(
    string Name,
    string ClassName,
    int Level,
    int Floor,
    string Difficulty,
    int Turns,
    long Coins,
    int Deaths,
    long PlaySeconds,
    bool Lost,
    bool Conquered,
    DateTimeOffset UpdatedAt);
