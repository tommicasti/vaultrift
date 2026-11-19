namespace Vaultrift.Engine.World;

public static class Tiles
{
    public static readonly TileType[] Currents =
        [TileType.CurrentUp, TileType.CurrentDown, TileType.CurrentLeft, TileType.CurrentRight];

    public static bool IsWalkable(TileType tile) =>
        tile is not (TileType.Wall or TileType.Obstacle or TileType.Crystal);

    public static Direction? CurrentDirection(TileType tile) => tile switch
    {
        TileType.CurrentUp => Direction.Up,
        TileType.CurrentDown => Direction.Down,
        TileType.CurrentLeft => Direction.Left,
        TileType.CurrentRight => Direction.Right,
        _ => null
    };

    public static TileType Current(Direction direction) => direction switch
    {
        Direction.Up => TileType.CurrentUp,
        Direction.Down => TileType.CurrentDown,
        Direction.Left => TileType.CurrentLeft,
        Direction.Right => TileType.CurrentRight,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction.")
    };
}
