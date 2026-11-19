using Vaultrift.Engine.World;

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
        new("swamp", "Swamp", "spider-queen", ["slime", "spider", "bat"], 0.10),
        new("volcano", "Volcano", "magma-golem", ["salamander", "fire-imp", "bat"], 0.05, new TerrainSpec(TileType.Lava, 0.10)),
        new("ruins", "Sunken Ruins", "kraken", ["crab", "eel", "slime"], 0.05, new TerrainSpec(TileType.Water, 0.25)),
        new("mines", "Crystal Mines", "crystal-golem", ["gem-beetle", "shardling", "bat"], 0.03,
            new TerrainSpec(TileType.Crystal, 0.12, TerrainShape.Cluster)),
        new("fungal", "Fungal Grotto", "mushroom-king", ["myconid", "puffball", "spider"], 0.08,
            new TerrainSpec(TileType.Spores, 0.15)),
        new("sky", "Sky Temple", "storm-griffin", ["harpy", "gargoyle", "bat"], 0.05,
            new TerrainSpec(TileType.CurrentRight, 0.8, TerrainShape.Stream))
    ];

    public static Biome Get(string id) =>
        All.FirstOrDefault(biome => biome.Id == id)
        ?? throw new KeyNotFoundException($"Unknown biome '{id}'.");
}
