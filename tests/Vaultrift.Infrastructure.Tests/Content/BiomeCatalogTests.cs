using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;

namespace Vaultrift.Infrastructure.Tests.Content;

public class BiomeCatalogTests
{
    [Fact]
    public void All_IdsAreUnique()
    {
        var ids = BiomeCatalog.All.Select(biome => biome.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void All_HasEnoughBiomesToNeverRepeatOneTwiceInARow()
    {
        Assert.True(BiomeCatalog.All.Count >= 2);
    }

    [Fact]
    public void EveryBiome_HasItsOwnBoss()
    {
        var bossIds = BiomeCatalog.All.Select(biome => biome.BossId).ToList();

        Assert.Equal(bossIds.Count, bossIds.Distinct().Count());
        Assert.All(bossIds, bossId => Assert.True(BestiaryCatalog.Get(bossId).IsBoss));
    }

    [Fact]
    public void EveryBiome_SpawnsOnlyRegularMonsters()
    {
        Assert.All(
            BiomeCatalog.All.SelectMany(biome => biome.MonsterIds),
            monsterId => Assert.False(BestiaryCatalog.Get(monsterId).IsBoss));
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => BiomeCatalog.Get("atlantis"));
    }

    [Theory]
    [InlineData("volcano", TileType.Lava)]
    [InlineData("ruins", TileType.Water)]
    public void TerrainBiomes_BringTheirOwnTerrain(string biomeId, TileType tile)
    {
        Assert.Equal(tile, BiomeCatalog.Get(biomeId).Terrain?.Tile);
    }
}
