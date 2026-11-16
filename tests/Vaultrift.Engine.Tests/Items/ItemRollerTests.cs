using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;

namespace Vaultrift.Engine.Tests.Items;

public class ItemRollerTests
{
    private static readonly WeaponDefinition RareSword = new(
        "test-rare-sword", "Rare Sword", Rarity.Rare, WeaponType.Sword, [new StatModifier(StatType.Attack, 5)]);

    private static readonly WeaponDefinition PlainLegendary = new(
        "test-plain-legendary", "Plain Legend", Rarity.Legendary, WeaponType.Sword,
        [new StatModifier(StatType.Attack, 5)], "Plain.");

    private static readonly WeaponDefinition WindLegendary = new(
        "test-wind-legendary", "Wind Legend", Rarity.Legendary, WeaponType.Bow,
        [new StatModifier(StatType.Attack, 5)], "Breezy.", Element.Wind);

    private static readonly GearDefinition Cloak = new(
        "test-cloak", "Test Cloak", Rarity.Common, EquipmentKind.Cloak, [new StatModifier(StatType.Evasion, 1)]);

    [Fact]
    public void Weapon_RollBelowItsRarityChance_GetsAnElement()
    {
        var item = ItemRoller.Roll(RareSword, new FixedRandom(0.34, 0.60));

        Assert.Equal(Element.Lightning, item.Element);
    }

    [Fact]
    public void Weapon_RollAtItsRarityChance_HasNoElement()
    {
        var item = ItemRoller.Roll(RareSword, new FixedRandom(0.35));

        Assert.Null(item.Element);
    }

    [Theory]
    [InlineData(0.0, Element.Fire)]
    [InlineData(0.30, Element.Ice)]
    [InlineData(0.55, Element.Lightning)]
    [InlineData(0.99, Element.Wind)]
    public void Cloak_AlwaysGetsAnElement(double roll, Element expected)
    {
        Assert.Equal(expected, ItemRoller.Roll(Cloak, new FixedRandom(roll)).Element);
    }

    [Fact]
    public void Legendary_KeepsItsFixedElementWithoutRolling()
    {
        Assert.Equal(Element.Wind, ItemRoller.Roll(WindLegendary, new FixedRandom()).Element);
    }

    [Fact]
    public void Legendary_WithoutFixedElement_NeverRolls()
    {
        Assert.Null(ItemRoller.Roll(PlainLegendary, new FixedRandom()).Element);
    }

    [Fact]
    public void PotionsAndArmor_NeverRoll()
    {
        Assert.Null(ItemRoller.Roll(TestItems.HealingPotion, new FixedRandom()).Element);
        Assert.Null(ItemRoller.Roll(TestItems.Armor, new FixedRandom()).Element);
    }
}