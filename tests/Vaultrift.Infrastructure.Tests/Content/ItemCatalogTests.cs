using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Tests.Content;

public class ItemCatalogTests
{
    private static readonly EquipmentKind[] GearKinds =
        [EquipmentKind.Head, EquipmentKind.Body, EquipmentKind.Feet, EquipmentKind.Ring];

    private static IEnumerable<EquipmentDefinition> Equipment => ItemCatalog.All.OfType<EquipmentDefinition>();

    private static IEnumerable<WeaponDefinition> Weapons => ItemCatalog.All.OfType<WeaponDefinition>();

    private static IEnumerable<GearDefinition> Gear => ItemCatalog.All.OfType<GearDefinition>();

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
    public void Gear_IsNeverAWeapon()
    {
        Assert.All(Gear, item => Assert.NotEqual(EquipmentKind.Weapon, item.Kind));
    }

    [Theory]
    [InlineData(EquipmentKind.Head, StatType.Defense)]
    [InlineData(EquipmentKind.Body, StatType.Defense)]
    [InlineData(EquipmentKind.Feet, StatType.Evasion)]
    public void Gear_GivesThePrincipalBonusOfItsKind(EquipmentKind kind, StatType principal)
    {
        Assert.All(
            Gear.Where(item => item.Kind == kind),
            item => Assert.Contains(item.Modifiers, modifier => modifier.Stat == principal));
    }

    [Fact]
    public void EveryWeaponType_ExistsInEveryRarity()
    {
        foreach (var type in Enum.GetValues<WeaponType>())
            foreach (var rarity in Enum.GetValues<Rarity>())
                Assert.Contains(Weapons, weapon => weapon.WeaponType == type && weapon.Rarity == rarity);
    }

    [Fact]
    public void EveryGearKind_ExistsInEveryRarity()
    {
        foreach (var kind in GearKinds)
            foreach (var rarity in Enum.GetValues<Rarity>())
                Assert.Contains(Gear, item => item.Kind == kind && item.Rarity == rarity);
    }

    [Fact]
    public void EveryEquipmentType_HasThreeLegendaries()
    {
        foreach (var type in Enum.GetValues<WeaponType>())
            Assert.Equal(3, Weapons.Count(weapon => weapon.WeaponType == type && weapon.Rarity == Rarity.Legendary));

        foreach (var kind in GearKinds)
            Assert.Equal(3, Gear.Count(item => item.Kind == kind && item.Rarity == Rarity.Legendary));
    }

    [Fact]
    public void Legendaries_HaveADescription()
    {
        Assert.All(
            ItemCatalog.All.Where(item => item.Rarity == Rarity.Legendary),
            item => Assert.False(string.IsNullOrWhiteSpace(item.Description)));
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
        var item = ItemCatalog.Get("sword-vaultbreaker");

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