using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.World;

public static class FloorGenerator
{
    public const int DefaultWidth = 48;
    public const int DefaultHeight = 32;
    public const int MinRooms = 6;
    public const int MaxRooms = 10;
    public const int RoomMargin = 2;

    private const int MinRoomWidth = 5;
    private const int MaxRoomWidth = 11;
    private const int MinRoomHeight = 4;
    private const int MaxRoomHeight = 8;
    private const int PlacementAttempts = 300;
    private const int ExtraCorridors = 2;

    public static FloorLayout Generate(IRandom random, int width = DefaultWidth, int height = DefaultHeight)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (width < MaxRoomWidth + 2 || height < MaxRoomHeight + 2)
            throw new ArgumentOutOfRangeException(nameof(width), "The floor is too small to hold a room.");

        while (true)
        {
            var rooms = PlaceRooms(random, width, height);

            if (rooms.Count >= MinRooms)
                return Build(random, width, height, rooms);
        }
    }

    public static Dictionary<Position, int> WalkingDistances(DungeonMap map, Position start)
    {
        ArgumentNullException.ThrowIfNull(map);

        var distances = new Dictionary<Position, int> { [start] = 0 };
        var queue = new Queue<Position>([start]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var direction in Enum.GetValues<Direction>())
            {
                var next = current.Move(direction);

                if (!map.IsWalkable(next) || distances.ContainsKey(next))
                    continue;

                distances[next] = distances[current] + 1;
                queue.Enqueue(next);
            }
        }

        return distances;
    }

    private static List<Room> PlaceRooms(IRandom random, int width, int height)
    {
        var rooms = new List<Room>();

        for (var attempt = 0; attempt < PlacementAttempts && rooms.Count < MaxRooms; attempt++)
        {
            var roomWidth = random.Next(MinRoomWidth, MaxRoomWidth + 1);
            var roomHeight = random.Next(MinRoomHeight, MaxRoomHeight + 1);
            var room = new Room(
                random.Next(1, width - roomWidth),
                random.Next(1, height - roomHeight),
                roomWidth,
                roomHeight);

            if (rooms.All(other => !room.Overlaps(other, RoomMargin)))
                rooms.Add(room);
        }

        return rooms.OrderBy(room => room.Center.X).ThenBy(room => room.Center.Y).ToList();
    }

    private static FloorLayout Build(IRandom random, int width, int height, List<Room> rooms)
    {
        var tiles = new TileType[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                tiles[y, x] = TileType.Wall;
        }

        foreach (var tile in rooms.SelectMany(room => room.Tiles()))
            tiles[tile.Y, tile.X] = TileType.Floor;

        for (var index = 1; index < rooms.Count; index++)
            CarveCorridor(tiles, rooms[index - 1].Center, rooms[index].Center, random);

        for (var extra = 0; extra < ExtraCorridors; extra++)
        {
            var from = random.Pick(rooms);
            var to = random.Pick(rooms);

            if (from != to)
                CarveCorridor(tiles, from.Center, to.Center, random);
        }

        var map = new DungeonMap(tiles);
        var startRoom = rooms[0];
        var distances = WalkingDistances(map, startRoom.Center);
        var bossRoom = rooms.Skip(1).MaxBy(room => distances[room.Center])!;

        return new FloorLayout(map, rooms, startRoom, bossRoom);
    }

    private static void CarveCorridor(TileType[,] tiles, Position from, Position to, IRandom random)
    {
        var corner = random.NextDouble() < 0.5
            ? new Position(to.X, from.Y)
            : new Position(from.X, to.Y);

        CarveLine(tiles, from, corner);
        CarveLine(tiles, corner, to);
    }

    private static void CarveLine(TileType[,] tiles, Position from, Position to)
    {
        var stepX = Math.Sign(to.X - from.X);
        var stepY = Math.Sign(to.Y - from.Y);
        var current = from;

        tiles[current.Y, current.X] = TileType.Floor;

        while (current != to)
        {
            current = new Position(current.X + stepX, current.Y + stepY);
            tiles[current.Y, current.X] = TileType.Floor;
        }
    }
}