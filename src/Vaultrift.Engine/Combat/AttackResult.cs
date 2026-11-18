using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Combat;

public readonly record struct AttackResult(
    int Damage,
    bool IsCritical,
    bool IsDodged = false,
    Element? Element = null,
    Effectiveness Effectiveness = Effectiveness.Neutral,
    bool IsBlocked = false)
{
    public static AttackResult Dodged { get; } = new(0, false, true);

    public static AttackResult Blocked { get; } = new(0, false, IsBlocked: true);

    public bool Missed => IsDodged || IsBlocked;
}
