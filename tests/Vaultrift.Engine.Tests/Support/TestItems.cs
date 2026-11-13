using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Tests.Support;

public static class TestItems
{
    public static WeaponDefinition Sword { get; } =
        new("test-sword", "Test Sword", Rarity.Common, WeaponType.Sword, [new StatModifier(StatType.Attack, 3)]);

    public static WeaponDefinition Axe { get; } =
        new("test-axe", "Test Axe", Rarity.Common, WeaponType.Axe, [new StatModifier(StatType.Attack, 2)]);

    public static GearDefinition Armor { get; } =
        new("test-armor", "Test Armor", Rarity.Common, EquipmentKind.Body, [new StatModifier(StatType.Defense, 1)]);

    public static GearDefinition Ring { get; } =
        new("test-ring", "Test Ring", Rarity.Common, EquipmentKind.Ring, [new StatModifier(StatType.Attack, 1)]);

    public static PotionDefinition HealingPotion { get; } =
        new("test-healing", "Test Healing Potion", Rarity.Common, Healing: 10);

    public static PotionDefinition StrengthPotion { get; } =
        new("test-strength", "Test Strength Potion", Rarity.Rare, Boost: new StatModifier(StatType.Attack, 3), BoostTurns: 10);
}