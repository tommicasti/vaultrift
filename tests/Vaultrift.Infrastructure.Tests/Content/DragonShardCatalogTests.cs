using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Tests.Content;

public class DragonShardCatalogTests
{
    private static int Sum(EquipmentDefinition item, StatType stat) =>
        item.Modifiers.Where(modifier => modifier.Stat == stat).Sum(modifier => modifier.Amount);

    [Fact]
    public void EveryShard_IsABoundPlatinumPieceThatGrowsWithTheHero()
    {
        foreach (var shard in ItemCatalog.DragonShards)
        {
            Assert.Equal(Rarity.Platinum, shard.Rarity);
            Assert.True(shard.IsBound && shard.ScalesWithLevel);
            Assert.Same(shard, ItemCatalog.Get(shard.Id));
        }
    }

    [Theory]
    [InlineData("knight", new[] { "dragon-blade-2", "dragon-aegis-2" })]
    [InlineData("berserker", new[] { "dragon-shard-2" })]
    [InlineData("ranger", new[] { "dragon-bow-2" })]
    [InlineData("rogue", new[] { "dragon-fangs-2" })]
    [InlineData("mage", new[] { "dragon-staff-2-ice" })]
    public void EachClass_GetsItsOwnForm(string classId, string[] expected)
    {
        Assert.Equal(expected, ItemCatalog.DragonShardSet(classId, 2, Element.Ice).Select(item => item.Id));
    }

    [Fact]
    public void TheMagesStaff_CastsASpellOfTheRolledElement()
    {
        foreach (var element in Enum.GetValues<Element>())
        {
            var staff = Assert.IsType<WeaponDefinition>(Assert.Single(ItemCatalog.DragonShardSet("mage", 3, element)));
            Assert.Equal(element, staff.Spell?.Element);
        }
    }

    [Fact]
    public void TheShard_NeverDropsOrShowsUpInShops()
    {
        Assert.DoesNotContain(ItemCatalog.All, item => item.Rarity == Rarity.Platinum);
        Assert.DoesNotContain(ItemCatalog.Equipment, item => item.Rarity == Rarity.Platinum);
    }

    [Fact]
    public void EvenTheWeakestShardWeapon_HitsHarderThanAnyExoticOfItsKind()
    {
        foreach (var shard in ItemCatalog.DragonShards.OfType<WeaponDefinition>().Where(shard => shard.Id.EndsWith("-1") || shard.Id.Contains("-1-")))
        {
            var exotics = ItemCatalog.Exotics.OfType<WeaponDefinition>().Where(exotic => exotic.WeaponType == shard.WeaponType).ToList();
            if (exotics.Count > 0)
                Assert.True(Sum(shard, StatType.Attack) > exotics.Max(exotic => Sum(exotic, StatType.Attack)), shard.Id);
        }
    }

    [Fact]
    public void TheAegis_BlocksMoreThanAnyOtherShield()
    {
        var aegis = ItemCatalog.DragonShards.Single(item => item.Id == "dragon-aegis-1");
        var best = ItemCatalog.Equipment.OfType<EquipmentDefinition>().Where(item => item.Kind == EquipmentKind.Shield).Max(item => Sum(item, StatType.Block));

        Assert.True(Sum(aegis, StatType.Block) > best);
    }

    [Fact]
    public void OnlyTheAxes_FillBothHands_AndTheFangsAreASingleDagger()
    {
        for (var tier = 1; tier <= 3; tier++)
        {
            var fangs = Assert.IsType<WeaponDefinition>(ItemCatalog.Get($"dragon-fangs-{tier}"));
            Assert.Equal(WeaponType.Dagger, fangs.WeaponType);
            Assert.False(fangs.IsTwin);
            Assert.False(fangs.IsTwoHanded);

            var axes = Assert.IsType<WeaponDefinition>(ItemCatalog.Get($"dragon-shard-{tier}"));
            Assert.True(axes.IsTwin && axes.IsTwoHanded);
        }
    }

    [Fact]
    public void HigherTiers_AreStronger()
    {
        foreach (var form in new[] { "dragon-shard", "dragon-blade", "dragon-bow", "dragon-fangs" })
            Assert.True(Sum(ItemCatalog.DragonShards.Single(item => item.Id == $"{form}-3"), StatType.Attack) > Sum(ItemCatalog.DragonShards.Single(item => item.Id == $"{form}-2"), StatType.Attack));
    }
}
