using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public enum AttackStyle
{
    Strike,
    Stab,
    Sweep,
    Thrust,
    Shoot,
    Cast
}

public sealed record AttackProfile(AttackStyle Style, double Reach, double ArcDegrees, double CooldownSeconds, int MaxTargets)
{
    public const double FastCooldownFactor = 0.85;

    public static AttackProfile Strike { get; } = new(AttackStyle.Strike, 1.0, 120, 0.55, 1);

    public static AttackProfile Stab { get; } = new(AttackStyle.Stab, 0.9, 90, 0.42, 1);

    public static AttackProfile Sweep { get; } = new(AttackStyle.Sweep, 1.15, 170, 0.7, int.MaxValue);

    public static AttackProfile Thrust { get; } = new(AttackStyle.Thrust, 2.0, 50, 0.65, 2);

    public static AttackProfile Shoot { get; } = new(AttackStyle.Shoot, LiveRules.ShotRange, 0, 0.7, 1);

    public static AttackProfile Cast { get; } = new(AttackStyle.Cast, LiveRules.ShotRange, 0, 0.8, 1);

    public bool IsRanged => Style is AttackStyle.Shoot or AttackStyle.Cast;

    public double ArcCosine => Math.Cos(ArcDegrees / 2 * Math.PI / 180);

    public static AttackProfile For(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return (player.Weapon?.Definition as WeaponDefinition)?.WeaponType switch
        {
            WeaponType.Staff when player.ActiveSpell is not null => Cast,
            WeaponType.Bow => Shoot,
            WeaponType.Axe => Sweep,
            WeaponType.Spear => Thrust,
            WeaponType.Dagger => Stab,
            _ => Strike
        };
    }

    public double CooldownFor(Speed speed) => speed == Speed.Fast ? CooldownSeconds * FastCooldownFactor : CooldownSeconds;
}
