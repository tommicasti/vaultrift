using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.World;

public class DungeonMapTests
{
    private static readonly string[] Rows =
    [
        "#####",
        "#..##",
        "#...#",
        "#####"
    ];

    [Fact]
    public void Parse_ReadsDimensions()
    {
        var map = DungeonMap.Parse(Rows);

        Assert.Equal(5, map.Width);
        Assert.Equal(4, map.Height);
    }

    [Theory]
    [InlineData(0, 0, TileType.Wall)]
    [InlineData(1, 1, TileType.Floor)]
    [InlineData(3, 1, TileType.Wall)]
    [InlineData(3, 2, TileType.Floor)]
    public void Parse_ReadsTiles(int x, int y, TileType expected)
    {
        var map = DungeonMap.Parse(Rows);

        Assert.Equal(expected, map.GetTile(new Position(x, y)));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(0, 0, false)]
    [InlineData(-1, 0, false)]
    [InlineData(5, 0, false)]
    [InlineData(0, 4, false)]
    public void IsWalkable_IsTrueOnlyForFloorInsideTheMap(int x, int y, bool expected)
    {
        var map = DungeonMap.Parse(Rows);

        Assert.Equal(expected, map.IsWalkable(new Position(x, y)));
    }

    [Fact]
    public void GetTile_OutsideTheMap_Throws()
    {
        var map = DungeonMap.Parse(Rows);

        Assert.Throws<ArgumentOutOfRangeException>(() => map.GetTile(new Position(10, 10)));
    }

    [Fact]
    public void Parse_RowsWithDifferentLengths_Throws()
    {
        string[] rows = ["###", "##"];

        Assert.Throws<ArgumentException>(() => DungeonMap.Parse(rows));
    }

    [Fact]
    public void Parse_UnknownSymbol_Throws()
    {
        string[] rows = ["#?#"];

        Assert.Throws<ArgumentException>(() => DungeonMap.Parse(rows));
    }
}