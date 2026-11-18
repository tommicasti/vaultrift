using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.World;

public static class FloorGenerator
{

    public const int DefaultWidth = 64;
    public const int DefaultHeight = 44;
    public const int MinRooms = 10;
    public const int MaxRooms = 16;
    public const int RoomMargin = 2;

    private const int MinRoomWidth = 5;
    private const int MaxRoomWidth = 11;
    private const int MinRoomHeight = 4;
    private const int MaxRoomHeight = 8;
    private const int PlacementAttempts = 600;
    private static readonly Direction[] Directions = Enum.GetValues<Direction>();

    public static FloorLayout Generate(
        IRandom random,
        int width = DefaultWidth,
        int height = DefaultHeight,
        double obstacleDensity = 0)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfNegative(obstacleDensity);

        if (width < MaxRoomWidth + 2 || height < MaxRoomHeight + 2)
            throw new ArgumentOutOfRangeException(nameof(width), "The floor is too small to hold a room.");

        while (true)
        {
            var rooms = PlaceRooms(random, width, height);

            if (rooms.Count >= MinRooms)
                return Build(random, width, height, rooms, obstacleDensity);
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

            foreach (var direction in Directions)
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

    private static FloorLayout Build(IRandom random, int width, int height, List<Room> rooms, double obstacleDensity)
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

        for (var index = 2; index < rooms.Count; index++)
            CarveCorridor(tiles, rooms[index - 2].Center, rooms[index].Center, random);

        var startRoom = rooms[0];
        var distances = WalkingDistances(new DungeonMap(tiles), startRoom.Center);
        var bossRoom = rooms.Skip(1).MaxBy(room => distances[room.Center])!;

        if (obstacleDensity > 0)
            PlaceObstacles(tiles, rooms, startRoom, bossRoom, obstacleDensity, random);

        return new FloorLayout(new DungeonMap(tiles), rooms, startRoom, bossRoom);
    }

    private static void PlaceObstacles(
        TileType[,] tiles,
        List<Room> rooms,
        Room startRoom,
        Room bossRoom,
        double density,
        IRandom random)
    {
        var start = startRoom.Center;
        var reserved = new HashSet<Position>
        {
            bossRoom.Center,
            bossRoom.Center with { X = bossRoom.Center.X - 1 }
        };
        var walkable = CountReachable(tiles, start);

        foreach (var room in rooms.Where(room => room != startRoom))
        {
            var interior = room.Tiles()
                .Where(tile => tile.X > room.X && tile.X < room.X + room.Width - 1)
                .Where(tile => tile.Y > room.Y && tile.Y < room.Y + room.Height - 1)
                .Where(tile => !reserved.Contains(tile))
                .ToList();
            var attempts = (int)Math.Round(interior.Count * density);

            for (var attempt = 0; attempt < attempts && interior.Count > 0; attempt++)
            {
                var tile = random.Pick(interior);
                interior.Remove(tile);
                tiles[tile.Y, tile.X] = TileType.Obstacle;

                if (CountReachable(tiles, start) == walkable - 1)
                    walkable--;
                else
                    tiles[tile.Y, tile.X] = TileType.Floor;
            }
        }
    }

    private static int CountReachable(TileType[,] tiles, Position start)
    {
        var height = tiles.GetLength(0);
        var width = tiles.GetLength(1);
        var visited = new bool[height, width];
        var queue = new Queue<Position>([start]);
        var count = 0;
        visited[start.Y, start.X] = true;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            count++;

            foreach (var direction in Directions)
            {
                var next = current.Move(direction);

                if (next.X < 0 || next.Y < 0 || next.X >= width || next.Y >= height)
                    continue;

                if (visited[next.Y, next.X] || tiles[next.Y, next.X] != TileType.Floor)
                    continue;

                visited[next.Y, next.X] = true;
                queue.Enqueue(next);
            }
        }

        return count;
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