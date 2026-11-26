namespace Vaultrift.Engine.Realtime;

public sealed record HeroInput(Vector Move, bool Attack, Vector Aim)
{
    public static HeroInput Idle { get; } = new(Vector.Zero, false, Vector.Zero);

    public static HeroInput Of(double moveX, double moveY, bool attack, double aimX = 0, double aimY = 0)
    {
        var move = Finite(moveX, moveY);
        return new HeroInput(move.Length > 1 ? move.Normalized() : move, attack, Finite(aimX, aimY).Normalized());
    }

    private static Vector Finite(double x, double y) => new(double.IsFinite(x) ? x : 0, double.IsFinite(y) ? y : 0);
}
