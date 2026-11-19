using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.World;

public class TerrainShapeTests
{
    private static FloorLayout Generate(int seed, TerrainSpec terrain) =>
        FloorGenerator.Generate(new SystemRandom(new Random(seed)), obstacleDensity: 0.05, terrain: terrain);

    private static List<Position> TilesWhere(DungeonMap map, Func<Position, bool> predicate)
    {
        var tiles = new List<Position>();

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (predicate(new Position(x, y)))
                    tiles.Add(new Position(x, y));
            }
        }

        return tiles;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Crystals_NeverCutOffAnyPartOfTheFloor(int seed)
    {
        var layout = Generate(seed, new TerrainSpec(TileType.Crystal, 0.2, TerrainShape.Cluster));
        var map = layout.Map;

        Assert.NotEmpty(TilesWhere(map, map.IsCrystal));
        Assert.Equal(
            TilesWhere(map, map.IsWalkable).Count,
            FloorGenerator.WalkingDistances(map, layout.StartRoom.Center).Count);
    }

    [Fact]
    public void Crystals_StayAwayFromTheStartRoomAndTheBossSpot()
    {
        var layout = Generate(6, new TerrainSpec(TileType.Crystal, 0.2, TerrainShape.Cluster));
        var crystals = TilesWhere(layout.Map, layout.Map.IsCrystal);

        Assert.DoesNotContain(crystals, tile => layout.StartRoom.Contains(tile));
        Assert.DoesNotContain(layout.BossRoom.Center, crystals);
    }

    [Fact]
    public void Streams_AreStraightLinesFlowingOneWay_InsideRooms()
    {
        var layout = Generate(7, new TerrainSpec(TileType.CurrentRight, 1.0, TerrainShape.Stream));
        var map = layout.Map;
        var currents = TilesWhere(map, tile => map.CurrentAt(tile) is not null);

        Assert.NotEmpty(currents);
        Assert.DoesNotContain(currents, tile => layout.StartRoom.Contains(tile));

        foreach (var room in layout.Rooms.Where(room => room != layout.StartRoom))
        {
            var inRoom = currents.Where(room.Contains).ToList();

            if (inRoom.Count == 0)
                continue;

            var flow = map.CurrentAt(inRoom[0]);
            Assert.All(inRoom, tile => Assert.Equal(flow, map.CurrentAt(tile)));
            Assert.True(inRoom.Select(tile => tile.X).Distinct().Count() == 1 || inRoom.Select(tile => tile.Y).Distinct().Count() == 1);
        }
    }

    [Fact]
    public void Streams_WithoutDensity_NeverAppear()
    {
        var layout = Generate(7, new TerrainSpec(TileType.CurrentRight, 0, TerrainShape.Stream));

        Assert.Empty(TilesWhere(layout.Map, tile => layout.Map.CurrentAt(tile) is not null));
    }

    [Fact]
    public void Spores_PoolLikeWater()
    {
        var layout = Generate(8, new TerrainSpec(TileType.Spores, 0.2));

        Assert.NotEmpty(TilesWhere(layout.Map, layout.Map.IsSpores));
    }
}
