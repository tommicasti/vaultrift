namespace Vaultrift.Engine.Combat;

public readonly record struct AttackResult(int Damage, bool IsCritical, bool IsDodged = false)
{
    public static AttackResult Dodged { get; } = new(0, false, true);
}