namespace Vaultrift.Engine.Items;

public static class ItemPrices
{
    public const int BuyMultiplier = 4;
    public const int ConsumableDivisor = 2;

    public static double FloorFactor(int floor) => 1 + Math.Max(0, floor - 1) / 10.0;

    public static int SellValue(ItemDefinition item, int floor = 1)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is WeaponDefinition { IsSpare: true })
            return 0;

        var value = item.Rarity switch
        {
            Rarity.Common => 6,
            Rarity.Rare => 18,
            Rarity.Epic => 45,
            Rarity.Legendary => 120,
            Rarity.Exotic => 300,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item.Rarity, "Unknown rarity.")
        };

        if (item is PotionDefinition or ScrollDefinition or RuneDefinition)
            value /= ConsumableDivisor;

        return (int)Math.Round(value * FloorFactor(floor), MidpointRounding.AwayFromZero);
    }

    public static int BuyPrice(ItemDefinition item, int floor = 1) => SellValue(item, floor) * BuyMultiplier;
}
