using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Tests.Content;

public class ItemCatalogTests
{
    private static IEnumerable<EquipmentDefinition> Equipment => ItemCatalog.All.OfType<EquipmentDefinition>();

    private static IEnumerable<WeaponDefinition> Weapons => ItemCatalog.All.OfType<WeaponDefinition>();

    private static IEnumerable<PotionDefinition> Potions => ItemCatalog.All.OfType<PotionDefinition>();

    [Fact]
    public void All_IdsAreUnique()
    {
        var duplicates = ItemCatalog.All
            .GroupBy(item => item.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Equipment_ModifierCountMatchesRarity()
    {
        Assert.All(Equipment, item => Assert.Equal(ExpectedModifierCount(item.Rarity), item.Modifiers.Count));
    }

    [Fact]
    public void Equipment_AllModifiersArePositive()
    {
        Assert.All(Equipment.SelectMany(item => item.Modifiers), modifier => Assert.True(modifier.Amount > 0));
    }

    [Fact]
    public void Weapons_GiveAttack()
    {
        Assert.All(Weapons, weapon => Assert.Contains(weapon.Modifiers, modifier => modifier.Stat == StatType.Attack));
    }

    [Fact]
    public void Armors_GiveDefense()
    {
        Assert.All(
            ItemCatalog.All.OfType<ArmorDefinition>(),
            armor => Assert.Contains(armor.Modifiers, modifier => modifier.Stat == StatType.Defense));
    }

    [Fact]
    public void EveryWeaponType_ExistsInEveryRarity()
    {
        foreach (var type in Enum.GetValues<WeaponType>())
            foreach (var rarity in Enum.GetValues<Rarity>())
                Assert.Contains(Weapons, weapon => weapon.WeaponType == type && weapon.Rarity == rarity);
    }

    [Fact]
    public void Potions_HaveExactlyOneEffect()
    {
        Assert.All(Potions, potion => Assert.True((potion.Healing > 0) ^ (potion.Boost is not null)));
    }

    [Fact]
    public void BoostPotions_LastAtLeastOneTurn()
    {
        Assert.All(Potions.Where(potion => potion.Boost is not null), potion => Assert.True(potion.BoostTurns > 0));
    }

    [Fact]
    public void Get_KnownId_ReturnsTheItem()
    {
        var item = ItemCatalog.Get("sword-legendary");

        Assert.Equal("Vaultbreaker", item.Name);
        Assert.Equal(Rarity.Legendary, item.Rarity);
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => ItemCatalog.Get("sword-imaginary"));
    }

    private static int ExpectedModifierCount(Rarity rarity) => rarity switch
    {
        Rarity.Common or Rarity.Rare => 1,
        Rarity.Epic => 2,
        Rarity.Legendary => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };
}