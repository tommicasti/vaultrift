using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.Combat;

public sealed class DamageCalculator
{
    public const double CriticalChance = 0.10;
    public const int CriticalMultiplier = 2;
    public const int MinimumDamage = 1;
    public const int MaxEvasion = 50;


    private readonly IRandom _random;

    public DamageCalculator(IRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public AttackResult Calculate(Stats attacker, Stats defender)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);

        if (IsDodged(defender))
            return AttackResult.Dodged;

        var baseDamage = Math.Max(MinimumDamage, attacker.Attack - defender.Defense);
        var isCritical = _random.NextDouble() < CriticalChance;
        var damage = isCritical ? baseDamage * CriticalMultiplier : baseDamage;

        return new AttackResult(damage, isCritical);
    }

    private bool IsDodged(Stats defender)
    {
        var evasion = Math.Min(defender.Evasion, MaxEvasion);

        return evasion > 0 && _random.NextDouble() < evasion / 100.0;
    }
}