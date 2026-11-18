namespace Vaultrift.Engine.World;

public sealed record Room(int X, int Y, int Width, int Height)
{
    public Position Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Position position) =>
        position.X >= X && position.X < X + Width &&
        position.Y >= Y && position.Y < Y + Height;

    public bool Overlaps(Room other, int margin) =>
        X - margin < other.X + other.Width && other.X - margin < X + Width &&
        Y - margin < other.Y + other.Height && other.Y - margin < Y + Height;

    public IEnumerable<Position> Tiles()
    {
        for (var y = Y; y < Y + Height; y++)
        {
            for (var x = X; x < X + Width; x++)
                yield return new Position(x, y);
        }
    }
}