using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Tests.Support;

public static class TestGear
{
    public static WeaponDefinition Dagger { get; } =
        new("test-dagger", "Test Dagger", Rarity.Common, WeaponType.Dagger, [new StatModifier(StatType.Attack, 2)]);

    public static WeaponDefinition Spear { get; } =
        new("test-spear", "Test Spear", Rarity.Common, WeaponType.Spear, [new StatModifier(StatType.Attack, 4)]);

    public static WeaponDefinition Bow { get; } =
        new("test-bow", "Test Bow", Rarity.Common, WeaponType.Bow, [new StatModifier(StatType.Attack, 3)]);

    public static GearDefinition Shield(int block = 15) =>
        new("test-shield", "Test Shield", Rarity.Common, EquipmentKind.Shield,
            [new StatModifier(StatType.Defense, 1), new StatModifier(StatType.Block, block)]);

    public static GearDefinition Ring(EffectKind kind, int amount, params StatModifier[] modifiers) =>
        new($"test-ring-{kind}", $"Test {kind} Ring", Rarity.Rare, EquipmentKind.Ring, modifiers)
        {
            Effects = [new ItemEffect(kind, amount)]
        };

    public static GearDefinition ClassMod(string classId, EffectKind kind, int amount) =>
        new($"test-mod-{kind}", $"Test {kind} Mod", Rarity.Rare, EquipmentKind.ClassMod, [])
        {
            ClassId = classId,
            Effects = [new ItemEffect(kind, amount)]
        };
}
