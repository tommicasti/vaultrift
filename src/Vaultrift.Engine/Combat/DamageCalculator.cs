using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.Combat;

public sealed class DamageCalculator
{
    public const int CriticalMultiplier = 2;
    public const int MinimumDamage = 1;
    public const int MaxEvasion = 50;
    public const int MaxCritical = 50;
    public const int MaxBlock = 30;

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

        if (IsBlocked(defender))
            return AttackResult.Blocked;

        var baseDamage = BaseDamage(attacker, defender);
        var isCritical = _random.NextDouble() < Math.Min(attacker.Critical, MaxCritical) / 100.0;
        var damage = isCritical ? baseDamage * CriticalMultiplier : baseDamage;

        return new AttackResult(damage, isCritical);
    }

    public static int BaseDamage(Stats attacker, Stats defender) =>
        Math.Max(MinimumDamage, attacker.Attack - defender.Defense);

    private bool IsBlocked(Stats defender)
    {
        var block = Math.Min(defender.Block, MaxBlock);

        return block > 0 && _random.NextDouble() < block / 100.0;
    }

    private bool IsDodged(Stats defender)
    {
        var evasion = Math.Min(defender.Evasion, MaxEvasion);

        return evasion > 0 && _random.NextDouble() < evasion / 100.0;
    }
}
