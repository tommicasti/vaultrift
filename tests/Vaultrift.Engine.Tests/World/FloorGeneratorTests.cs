using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.World;

public class FloorGeneratorTests
{
    private static FloorLayout Generate(int seed) => FloorGenerator.Generate(new SystemRandom(new Random(seed)));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Generate_CreatesAMapOfTheDefaultSize_WithEnoughRooms(int seed)
    {
        var layout = Generate(seed);

        Assert.Equal(FloorGenerator.DefaultWidth, layout.Map.Width);
        Assert.Equal(FloorGenerator.DefaultHeight, layout.Map.Height);
        Assert.InRange(layout.Rooms.Count, FloorGenerator.MinRooms, FloorGenerator.MaxRooms);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Generate_RoomsNeverOverlap(int seed)
    {
        var rooms = Generate(seed).Rooms;

        for (var first = 0; first < rooms.Count; first++)
        {
            for (var second = first + 1; second < rooms.Count; second++)
                Assert.False(rooms[first].Overlaps(rooms[second], margin: 1));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Generate_EveryFloorTileIsReachableFromTheStart(int seed)
    {
        var layout = Generate(seed);
        var reachable = FloorGenerator.WalkingDistances(layout.Map, layout.StartRoom.Center);
        var floorTiles = 0;

        for (var y = 0; y < layout.Map.Height; y++)
        {
            for (var x = 0; x < layout.Map.Width; x++)
            {
                if (layout.Map.IsWalkable(new Position(x, y)))
                    floorTiles++;
            }
        }

        Assert.Equal(floorTiles, reachable.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Generate_TheBossRoomIsTheFarthestFromTheStart(int seed)
    {
        var layout = Generate(seed);
        var distances = FloorGenerator.WalkingDistances(layout.Map, layout.StartRoom.Center);

        Assert.NotEqual(layout.StartRoom, layout.BossRoom);
        Assert.All(layout.Rooms, room => Assert.True(distances[room.Center] <= distances[layout.BossRoom.Center]));
    }

    [Fact]
    public void Generate_AddsLoops_SoFewCorridorTilesAreForcedBetweenStartAndBoss()
    {
        var forced = Enumerable.Range(1, 20).Select(ForcedCorridorTiles).ToList();

        Assert.True(forced.Average() < 5, $"On average {forced.Average()} corridor tiles cannot be avoided.");
    }

    [Fact]
    public void Generate_TheBorderIsAlwaysWall()
    {
        var map = Generate(4).Map;

        for (var x = 0; x < map.Width; x++)
        {
            Assert.False(map.IsWalkable(new Position(x, 0)));
            Assert.False(map.IsWalkable(new Position(x, map.Height - 1)));
        }

        for (var y = 0; y < map.Height; y++)
        {
            Assert.False(map.IsWalkable(new Position(0, y)));
            Assert.False(map.IsWalkable(new Position(map.Width - 1, y)));
        }
    }

    [Fact]
    public void Generate_WithTheSameSeed_IsIdentical()
    {
        Assert.Equal(Generate(8).Rooms, Generate(8).Rooms);
    }

    [Fact]
    public void Generate_WithoutDensity_PlacesNoObstacles()
    {
        Assert.Equal(0, CountObstacles(Generate(5).Map));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Generate_WithObstacles_KeepsEveryFloorTileReachable(int seed)
    {
        var layout = FloorGenerator.Generate(new SystemRandom(new Random(seed)), obstacleDensity: 0.3);
        var reachable = FloorGenerator.WalkingDistances(layout.Map, layout.StartRoom.Center);
        var floorTiles = 0;

        for (var y = 0; y < layout.Map.Height; y++)
        {
            for (var x = 0; x < layout.Map.Width; x++)
            {
                if (layout.Map.IsWalkable(new Position(x, y)))
                    floorTiles++;
            }
        }

        Assert.True(CountObstacles(layout.Map) > 0);
        Assert.Equal(floorTiles, reachable.Count);
    }

    [Fact]
    public void Generate_WithObstacles_KeepsTheStartRoomAndTheBossSpotsClear()
    {
        var layout = FloorGenerator.Generate(new SystemRandom(new Random(6)), obstacleDensity: 0.5);
        var bossCenter = layout.BossRoom.Center;

        Assert.All(layout.StartRoom.Tiles(), tile => Assert.True(layout.Map.IsWalkable(tile)));
        Assert.True(layout.Map.IsWalkable(bossCenter));
        Assert.True(layout.Map.IsWalkable(bossCenter with { X = bossCenter.X - 1 }));
    }

    private static int CountObstacles(DungeonMap map)
    {
        var count = 0;

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (map.GetTile(new Position(x, y)) == TileType.Obstacle)
                    count++;
            }
        }

        return count;
    }

    private static int ForcedCorridorTiles(int seed)
    {
        var layout = Generate(seed);
        var start = layout.StartRoom.Center;
        var boss = layout.BossRoom.Center;
        var forced = 0;

        for (var y = 0; y < layout.Map.Height; y++)
        {
            for (var x = 0; x < layout.Map.Width; x++)
            {
                var tile = new Position(x, y);

                if (!layout.Map.IsWalkable(tile) || layout.Rooms.Any(room => room.Contains(tile)))
                    continue;

                if (!CanWalk(layout.Map, start, boss, tile))
                    forced++;
            }
        }

        return forced;
    }

    private static bool CanWalk(DungeonMap map, Position from, Position to, Position blocked)
    {
        var seen = new HashSet<Position> { from };
        var queue = new Queue<Position>([from]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == to)
                return true;

            foreach (var direction in Enum.GetValues<Direction>())
            {
                var next = current.Move(direction);

                if (next != blocked && map.IsWalkable(next) && seen.Add(next))
                    queue.Enqueue(next);
            }
        }

        return false;
    }
}