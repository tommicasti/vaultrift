using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Support;

namespace Vaultrift.Engine.Tests.Items;

public class ItemInstanceTests
{
    private static readonly WeaponDefinition FireLegendary = new(
        "test-fire-legendary", "Test Flame", Rarity.Legendary, WeaponType.Sword,
        [new StatModifier(StatType.Attack, 5)], "Hot.", Element.Fire);

    private static readonly GearDefinition Cloak = new(
        "test-cloak", "Test Cloak", Rarity.Common, EquipmentKind.Cloak, [new StatModifier(StatType.Evasion, 1)]);

    [Fact]
    public void NewInstance_WithoutElement_KeepsTheDefinitionName()
    {
        var item = new ItemInstance(TestItems.Sword);

        Assert.Null(item.Element);
        Assert.Equal("Test Sword", item.Name);
        Assert.Equal(TestItems.Sword.Id, item.Id);
    }

    [Theory]
    [InlineData(Element.Fire, "Blazing Test Sword")]
    [InlineData(Element.Ice, "Frozen Test Sword")]
    [InlineData(Element.Lightning, "Shocking Test Sword")]
    [InlineData(Element.Wind, "Gusting Test Sword")]
    public void Weapon_WithElement_GetsAPrefix(Element element, string expected)
    {
        Assert.Equal(expected, new ItemInstance(TestItems.Sword, element).Name);
    }

    [Fact]
    public void Cloak_WithElement_GetsAResistancePrefix()
    {
        Assert.Equal("Fireproof Test Cloak", new ItemInstance(Cloak, Element.Fire).Name);
    }

    [Fact]
    public void Legendary_WithFixedElement_HasItWithoutPrefix()
    {
        var item = new ItemInstance(FireLegendary);

        Assert.Equal(Element.Fire, item.Element);
        Assert.Equal("Test Flame", item.Name);
    }

    [Fact]
    public void Legendary_WithADifferentElement_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ItemInstance(FireLegendary, Element.Ice));
    }

    [Fact]
    public void Potion_WithElement_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ItemInstance(TestItems.HealingPotion, Element.Fire));
    }

    [Fact]
    public void Armor_WithElement_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ItemInstance(TestItems.Armor, Element.Fire));
    }

    [Fact]
    public void Instances_WithSameDefinitionAndElement_AreEqual()
    {
        Assert.Equal(new ItemInstance(TestItems.Sword, Element.Ice), new ItemInstance(TestItems.Sword, Element.Ice));
        Assert.NotEqual(new ItemInstance(TestItems.Sword, Element.Ice), new ItemInstance(TestItems.Sword));
    }

    [Fact]
    public void Definition_ConvertsImplicitlyToAnInstanceWithoutElement()
    {
        ItemInstance item = TestItems.Sword;

        Assert.Same(TestItems.Sword, item.Definition);
        Assert.Null(item.Element);
    }
}