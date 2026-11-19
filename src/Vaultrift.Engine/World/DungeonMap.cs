namespace Vaultrift.Engine.World;

public sealed class DungeonMap
{
    private readonly TileType[,] _tiles;

    public DungeonMap(TileType[,] tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);

        if (tiles.GetLength(0) == 0 || tiles.GetLength(1) == 0)
            throw new ArgumentException("The map must have at least one tile.", nameof(tiles));

        _tiles = (TileType[,])tiles.Clone();
    }

    public int Width => _tiles.GetLength(1);

    public int Height => _tiles.GetLength(0);

    public bool IsInside(Position position) =>
        position.X >= 0 && position.X < Width &&
        position.Y >= 0 && position.Y < Height;

    public TileType GetTile(Position position)
    {
        if (!IsInside(position))
            throw new ArgumentOutOfRangeException(nameof(position), position, "The position is outside the map.");

        return _tiles[position.Y, position.X];
    }

    public bool IsWalkable(Position position) =>
        IsInside(position) && Tiles.IsWalkable(_tiles[position.Y, position.X]);

    public bool IsLava(Position position) =>
        IsInside(position) && _tiles[position.Y, position.X] == TileType.Lava;

    public bool IsWater(Position position) =>
        IsInside(position) && _tiles[position.Y, position.X] == TileType.Water;

    public bool IsCrystal(Position position) =>
        IsInside(position) && _tiles[position.Y, position.X] == TileType.Crystal;

    public bool IsSpores(Position position) =>
        IsInside(position) && _tiles[position.Y, position.X] == TileType.Spores;

    public Direction? CurrentAt(Position position) =>
        IsInside(position) ? Tiles.CurrentDirection(_tiles[position.Y, position.X]) : null;

    internal void SetTile(Position position, TileType tile)
    {
        if (!IsInside(position))
            throw new ArgumentOutOfRangeException(nameof(position), position, "The position is outside the map.");

        _tiles[position.Y, position.X] = tile;
    }

    public static DungeonMap Parse(IReadOnlyList<string> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0)
            throw new ArgumentException("The map must have at least one row.", nameof(rows));

        var width = rows[0].Length;
        var tiles = new TileType[rows.Count, width];

        for (var y = 0; y < rows.Count; y++)
        {
            if (rows[y].Length != width)
                throw new ArgumentException($"Row {y} has length {rows[y].Length}, expected {width}.", nameof(rows));

            for (var x = 0; x < width; x++)
            {
                tiles[y, x] = rows[y][x] switch
                {
                    '#' => TileType.Wall,
                    '.' => TileType.Floor,
                    'o' => TileType.Obstacle,
                    'L' => TileType.Lava,
                    '~' => TileType.Water,
                    'C' => TileType.Crystal,
                    'S' => TileType.Spores,
                    '^' => TileType.CurrentUp,
                    'v' => TileType.CurrentDown,
                    '<' => TileType.CurrentLeft,
                    '>' => TileType.CurrentRight,
                    var symbol => throw new ArgumentException($"Unknown symbol '{symbol}' at ({x}, {y}).", nameof(rows))
                };
            }
        }

        return new DungeonMap(tiles);
    }
}