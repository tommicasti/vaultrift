namespace Vaultrift.Infrastructure.Content;

public sealed record Biome(
    string Id,
    string Name,
    string BossId,
    IReadOnlyList<string> MonsterIds,
    double ObstacleDensity);