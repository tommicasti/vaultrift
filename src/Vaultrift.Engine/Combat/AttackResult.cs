using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Combat;

public readonly record struct AttackResult(
    int Damage,
    bool IsCritical,
    bool IsDodged = false,
    Element? Element = null,
    Effectiveness Effectiveness = Effectiveness.Neutral)
{
    public static AttackResult Dodged { get; } = new(0, false, true);
}