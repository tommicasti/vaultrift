using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Tests.Content;

public class DragonShardCatalogTests
{
    [Fact]
    public void EveryTier_IsABoundTwinPlatinumAxe()
    {
        foreach (var shard in ItemCatalog.DragonShards)
        {
            Assert.Equal(Rarity.Platinum, shard.Rarity);
            Assert.Equal(WeaponType.Axe, shard.WeaponType);
            Assert.True(shard.IsTwin && shard.IsBound && shard.ScalesWithLevel);
            Assert.Same(shard, ItemCatalog.Get(shard.Id));
        }
    }

    [Fact]
    public void TheShard_NeverDropsOrShowsUpInShops()
    {
        Assert.DoesNotContain(ItemCatalog.All, item => item.Rarity == Rarity.Platinum);
        Assert.DoesNotContain(ItemCatalog.Equipment, item => item.Rarity == Rarity.Platinum);
    }

    [Fact]
    public void EvenTheWeakestShard_HitsHarderThanAnyExotic()
    {
        static int AttackOf(WeaponDefinition weapon) =>
            weapon.Modifiers.Where(modifier => modifier.Stat == StatType.Attack).Sum(modifier => modifier.Amount);

        Assert.True(AttackOf(ItemCatalog.DragonShardFor(1)) > ItemCatalog.Exotics.Max(AttackOf));
        Assert.True(AttackOf(ItemCatalog.DragonShardFor(3)) > AttackOf(ItemCatalog.DragonShardFor(2)));
    }
}
