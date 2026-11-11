using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.World;

public class PositionTests
{
    [Theory]
    [InlineData(Direction.Up, 5, 4)]
    [InlineData(Direction.Down, 5, 6)]
    [InlineData(Direction.Left, 4, 5)]
    [InlineData(Direction.Right, 6, 5)]
    public void Move_ReturnsAdjacentPosition(Direction direction, int expectedX, int expectedY)
    {
        var start = new Position(5, 5);

        var result = start.Move(direction);

        Assert.Equal(new Position(expectedX, expectedY), result);
    }
}