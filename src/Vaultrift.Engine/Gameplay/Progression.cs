using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public static class Progression
{
    public const int CoinsBase = 2;
    public const int LevelsPerCoin = 2;
    public const int BossCoinMultiplier = 4;
    public const int CoinLossPercent = 25;

    public static int CoinsFor(Enemy enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);

        var coins = (CoinsBase + enemy.Level / LevelsPerCoin) * RarityCoinMultiplier(enemy.Rarity);

        return enemy.Kind.IsBoss || enemy.Kind.IsStationary ? coins * BossCoinMultiplier : coins;
    }

    public static int ChestCoins(int floor) => 4 + 2 * Math.Max(1, floor);

    private static int RarityCoinMultiplier(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1,
        Rarity.Rare => 2,
        Rarity.Epic => 3,
        Rarity.Legendary or Rarity.Exotic or Rarity.Platinum => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown rarity.")
    };

    public const int ExperiencePerLevel = 20;
    public const int PointsPerLevel = 3;
    public const int HealthPerPoint = 5;
    public const int AttackPerPoint = 1;
    public const int DefensePerPoint = 1;
    public const int EvasionPerPoint = 2;
    public const int CriticalPerPoint = 2;
    public const int ManaPerPoint = 5;
    public const int ManaRegenInterval = 2;
    public const int ManaRegenAmount = 1;
    public const int BossExperienceMultiplier = 2;

    public static int ExperienceToNextLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        return ExperiencePerLevel * level * level;
    }

    public static int ExperienceFor(Enemy enemy, int wisdom = 0, int playerLevel = 0)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentOutOfRangeException.ThrowIfNegative(wisdom);

        var experience = enemy.Kind.BaseStats.MaxHealth / 2.0
            * RarityExperienceMultiplier(enemy.Rarity)
            * enemy.Level
            * (enemy.Kind.IsBoss ? BossExperienceMultiplier : 1)
            * (1 + wisdom / 100.0)
            * LevelGapFactor(playerLevel - enemy.Level);

        return Math.Max(1, (int)Math.Round(experience, MidpointRounding.AwayFromZero));
    }

    public const double ExperienceLossPerLevelAbove = 0.25;
    public const double MinimumExperienceFactor = 0.1;

    public static double LevelGapFactor(int levelsAbove) =>
        levelsAbove <= 0 ? 1.0 : Math.Max(MinimumExperienceFactor, 1 - ExperienceLossPerLevelAbove * levelsAbove);

    public static int RarityExperienceMultiplier(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1,
        Rarity.Rare => 2,
        Rarity.Epic => 3,
        Rarity.Legendary or Rarity.Exotic or Rarity.Platinum => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown rarity.")
    };

    public static Stats Apply(Stats baseStats, IReadOnlyDictionary<StatPoint, int> spent)
    {
        ArgumentNullException.ThrowIfNull(baseStats);
        ArgumentNullException.ThrowIfNull(spent);

        int Points(StatPoint stat) => spent.TryGetValue(stat, out var points) ? points : 0;

        return new Stats(
            baseStats.MaxHealth + Points(StatPoint.Health) * HealthPerPoint,
            baseStats.Attack + Points(StatPoint.Attack) * AttackPerPoint,
            baseStats.Defense + Points(StatPoint.Defense) * DefensePerPoint,
            Math.Min(100, baseStats.Evasion + Points(StatPoint.Evasion) * EvasionPerPoint),
            Math.Min(100, baseStats.Critical + Points(StatPoint.Critical) * CriticalPerPoint),
            baseStats.Block,
            baseStats.MaxMana + Points(StatPoint.Mana) * ManaPerPoint);
    }
}
