namespace Vaultrift.Infrastructure.Content;

public static class BiomeCatalog
{
    public static IReadOnlyList<Biome> All { get; } =
    [
        new("crypt", "Crypt", "rat-king", ["rat", "bat", "ghost"], 0.06),
        new("forest", "Forest", "troll", ["spider", "bat", "slime"], 0.12),
        new("desert", "Desert", "dune-scorpion", ["rat", "spider", "cyclops"], 0.05),
        new("snow", "Frozen Peaks", "frost-yeti", ["bat", "ghost", "cyclops"], 0.08),
        new("castle", "Castle", "black-knight", ["rat", "ghost", "cyclops"], 0.06),
        new("swamp", "Swamp", "spider-queen", ["slime", "spider", "bat"], 0.10)
    ];

    public static Biome Get(string id) =>
        All.FirstOrDefault(biome => biome.Id == id)
        ?? throw new KeyNotFoundException($"Unknown biome '{id}'.");
}