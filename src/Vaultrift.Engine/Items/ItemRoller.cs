using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.Items;

public static class ItemRoller
{
    public static double WeaponElementChance(Rarity rarity) => rarity switch
    {
        Rarity.Common => 0.20,
        Rarity.Rare => 0.35,
        Rarity.Epic => 0.50,
        Rarity.Legendary => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown rarity.")
    };

    public static ItemInstance Roll(ItemDefinition definition, IRandom random)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(random);

        return definition switch
        {
            EquipmentDefinition { FixedElement: not null } => new ItemInstance(definition),
            GearDefinition { Kind: EquipmentKind.Cloak } => new ItemInstance(definition, RandomElement(random)),
            WeaponDefinition { Rarity: not Rarity.Legendary } when random.NextDouble() < WeaponElementChance(definition.Rarity)
                => new ItemInstance(definition, RandomElement(random)),
            _ => new ItemInstance(definition)
        };
    }

    private static Element RandomElement(IRandom random)
    {
        var elements = Enum.GetValues<Element>();
        var index = (int)(random.NextDouble() * elements.Length);

        return elements[Math.Min(index, elements.Length - 1)];
    }
}