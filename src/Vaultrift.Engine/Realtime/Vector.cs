namespace Vaultrift.Engine.Realtime;

public readonly record struct Vector(double X, double Y)
{
    public static Vector Zero { get; } = new(0, 0);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public bool IsZero => X == 0 && Y == 0;

    public Vector Normalized()
    {
        var length = Length;
        return length < 1e-9 ? Zero : new Vector(X / length, Y / length);
    }

    public double Dot(Vector other) => X * other.X + Y * other.Y;

    public Vector Rotated(double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new Vector(X * cos - Y * sin, X * sin + Y * cos);
    }

    public double DistanceTo(Vector other) => (other - this).Length;

    public static Vector operator +(Vector left, Vector right) => new(left.X + right.X, left.Y + right.Y);

    public static Vector operator -(Vector left, Vector right) => new(left.X - right.X, left.Y - right.Y);

    public static Vector operator *(Vector vector, double factor) => new(vector.X * factor, vector.Y * factor);
}
