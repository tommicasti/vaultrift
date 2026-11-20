using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Tests.Content;

public class BestiaryCatalogTests
{
    [Fact]
    public void All_IdsAreUnique()
    {
        var ids = BestiaryCatalog.All.Select(kind => kind.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void All_EvasionStaysBelowTheCap()
    {
        Assert.All(BestiaryCatalog.All, kind => Assert.True(kind.BaseStats.Evasion <= DamageCalculator.MaxEvasion));
    }

    [Theory]
    [InlineData(Speed.Slow)]
    [InlineData(Speed.Normal)]
    [InlineData(Speed.Fast)]
    public void EverySpeed_HasAtLeastOneKind(Speed speed)
    {
        Assert.Contains(BestiaryCatalog.All, kind => kind.Speed == speed);
    }

    [Fact]
    public void Bosses_AreMarked_AndLeftOutOfTheMonsters()
    {
        var bossIds = BestiaryCatalog.All.Where(kind => kind.IsBoss).Select(kind => kind.Id).ToList();

        Assert.Equal(
            new List<string> { "rat-king", "spider-queen", "troll", "dune-scorpion", "frost-yeti", "black-knight", "magma-golem", "kraken",
                "crystal-golem", "mushroom-king", "storm-griffin", "custode" },
            bossIds);
        Assert.DoesNotContain(BestiaryCatalog.Monsters, kind => kind.IsBoss);
    }

    [Fact]
    public void EveryElement_IsSomeKindsWeakness()
    {
        foreach (var element in Enum.GetValues<Element>())
            Assert.Contains(BestiaryCatalog.All, kind => kind.Weakness == element);
    }

    [Fact]
    public void Get_KnownId_ReturnsTheKind()
    {
        Assert.Equal("Cyclops", BestiaryCatalog.Get("cyclops").Name);
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => BestiaryCatalog.Get("dragon"));
    }

    [Fact]
    public void TheMimic_NeverMoves_AndNeverSpawnsInABiome()
    {
        Assert.True(BestiaryCatalog.Get("mimic").IsStationary);
        Assert.DoesNotContain(BiomeCatalog.All, biome => biome.MonsterIds.Contains("mimic") || biome.BossId == "mimic");
    }

    [Theory]
    [InlineData("magma-golem", BossPower.LavaRing)]
    [InlineData("kraken", BossPower.TentaclePull)]
    [InlineData("crystal-golem", BossPower.CrystalBloom)]
    [InlineData("mushroom-king", BossPower.SummonMinions)]
    [InlineData("storm-griffin", BossPower.Gust)]
    public void NewBosses_HaveTheirSpecialPower(string bossId, BossPower power)
    {
        Assert.Equal(power, BestiaryCatalog.Get(bossId).Power);
    }

    [Fact]
    public void VolcanoMonsters_AreAllFireproof()
    {
        var volcano = BiomeCatalog.Get("volcano");

        Assert.All(volcano.MonsterIds.Where(id => id != "bat"), id => Assert.True(BestiaryCatalog.Get(id).IsFireproof));
        Assert.True(BestiaryCatalog.Get(volcano.BossId).IsFireproof);
    }

    [Fact]
    public void TheMushroomKing_SummonsSporelings_WhichNeverSpawnOnTheirOwn()
    {
        Assert.Equal("sporeling", BestiaryCatalog.Get("mushroom-king").Minion?.Id);
        Assert.DoesNotContain(BiomeCatalog.All, biome => biome.MonsterIds.Contains("sporeling"));
    }

    [Fact]
    public void TheCustode_ChangesPowerAsItLosesHealth()
    {
        var custode = BestiaryCatalog.Get("custode");

        Assert.Equal(
            [BossPower.LavaRing, BossPower.SummonMinions, BossPower.Gust],
            custode.Phases.Select(phase => phase.Power).ToList());
        Assert.Equal("drakeling", custode.Minion?.Id);
        Assert.Equal(Element.Ice, custode.Weakness);
        Assert.DoesNotContain(BiomeCatalog.All, biome => biome.MonsterIds.Contains("drakeling") || biome.BossId == "custode");
    }
}
