namespace Vaultrift.Engine.Gameplay;

public static class AbilityRules
{
    public const int BulwarkTurns = 3;
    public const int FuryTurns = 5;
    public const int InvisibilityTurns = 5;
    public const double FuryAttackMultiplier = 1.5;
    public const int PowerShotDamageMultiplier = 2;
    public const int AmbushDamageMultiplier = 2;

    public static int Cooldown(ClassAbility ability) => ability switch
    {
        ClassAbility.Bulwark => 10,
        ClassAbility.Fury => 10,
        ClassAbility.PowerShot => 6,
        ClassAbility.Invisibility => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, "Unknown ability.")
    };
}
