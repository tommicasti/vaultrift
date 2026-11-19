namespace Vaultrift.Engine.Gameplay;

public static class AbilityRules
{
    public const int BulwarkTurns = 3;
    public const int FuryTurns = 5;
    public const int InvisibilityTurns = 5;
    public const double FuryAttackMultiplier = 1.5;
    public const int PowerShotDamageMultiplier = 2;
    public const int AmbushDamageMultiplier = 2;
    public const int PowerShotMasteryReductionPercent = 30;
    public const int MaxCooldownReductionPercent = 40;
    public const int OverloadAreaBonus = 1;

    public static int Cooldown(ClassAbility ability) => ability switch
    {
        ClassAbility.Bulwark => 18,
        ClassAbility.Fury => 20,
        ClassAbility.PowerShot => 16,
        ClassAbility.Invisibility => 24,
        ClassAbility.Overload => 20,
        _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, "Unknown ability.")
    };

    public static int ReducedCooldown(ClassAbility ability, int reductionPercent)
    {
        var percent = Math.Clamp(reductionPercent, 0, MaxCooldownReductionPercent);

        return Math.Max(1, (int)Math.Ceiling(Cooldown(ability) * (100 - percent) / 100.0));
    }
}
