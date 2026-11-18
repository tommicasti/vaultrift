using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public static class Progression
{
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

    public static int ExperienceFor(Enemy enemy, int wisdom = 0)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentOutOfRangeException.ThrowIfNegative(wisdom);

        var experience = enemy.Kind.BaseStats.MaxHealth / 2.0
            * RarityExperienceMultiplier(enemy.Rarity)
            * enemy.Level
            * (enemy.Kind.IsBoss ? BossExperienceMultiplier : 1)
            * (1 + wisdom / 100.0);

        return Math.Max(1, (int)Math.Round(experience, MidpointRounding.AwayFromZero));
    }

    public static int RarityExperienceMultiplier(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1,
        Rarity.Rare => 2,
        Rarity.Epic => 3,
        Rarity.Legendary => 5,
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
