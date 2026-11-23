using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public static class EnemyScaling
{
    public const double GrowthPerLevel = 0.15;
    public const double MonsterHealthGrowthPerLevel = 0.20;

    public static double RarityMultiplier(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1.0,
        Rarity.Rare => 1.3,
        Rarity.Epic => 1.6,
        Rarity.Legendary or Rarity.Exotic or Rarity.Platinum => 2.0,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown rarity.")
    };

    public static Stats Scale(Stats baseStats, Rarity rarity, int level, bool isBoss = false)
    {
        ArgumentNullException.ThrowIfNull(baseStats);
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);

        var factor = (1 + GrowthPerLevel * (level - 1)) * RarityMultiplier(rarity);
        var healthGrowth = isBoss ? GrowthPerLevel : MonsterHealthGrowthPerLevel;
        var healthFactor = (1 + healthGrowth * (level - 1)) * RarityMultiplier(rarity);

        return new Stats(
            Math.Max(1, Scaled(baseStats.MaxHealth, healthFactor)),
            Scaled(baseStats.Attack, factor),
            Scaled(baseStats.Defense, factor),
            baseStats.Evasion,
            baseStats.Critical);
    }

    private static int Scaled(int value, double factor) =>
        (int)Math.Round(value * factor, MidpointRounding.AwayFromZero);
}
