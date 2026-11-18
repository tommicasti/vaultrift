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
}