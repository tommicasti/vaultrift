using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class GameTests
{
    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#####",
        "#...#",
        "#.#.#",
        "#####"
    ]);

    [Fact]
    public void Constructor_SetsPlayerPosition()
    {
        var game = new Game(Map, new Position(1, 1));

        Assert.Equal(new Position(1, 1), game.PlayerPosition);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(10, 10)]
    public void Constructor_StartNotOnFloor_Throws(int x, int y)
    {
        Assert.Throws<ArgumentException>(() => new Game(Map, new Position(x, y)));
    }

    [Fact]
    public void Constructor_NullMap_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Game(null!, new Position(1, 1)));
    }

    [Fact]
    public void MovePlayer_ToFloor_MovesPlayer()
    {
        var game = new Game(Map, new Position(1, 1));

        var outcome = game.MovePlayer(Direction.Right);

        Assert.Equal(MoveOutcome.Moved, outcome);
        Assert.Equal(new Position(2, 1), game.PlayerPosition);
    }

    [Theory]
    [InlineData(1, 1, Direction.Up)]
    [InlineData(1, 1, Direction.Left)]
    [InlineData(2, 1, Direction.Down)]
    public void MovePlayer_IntoWall_DoesNotMovePlayer(int startX, int startY, Direction direction)
    {
        var start = new Position(startX, startY);
        var game = new Game(Map, start);

        var outcome = game.MovePlayer(direction);

        Assert.Equal(MoveOutcome.Blocked, outcome);
        Assert.Equal(start, game.PlayerPosition);
    }

    [Fact]
    public void MovePlayer_SeveralMoves_FollowsThePath()
    {
        var game = new Game(Map, new Position(1, 1));

        game.MovePlayer(Direction.Right);
        game.MovePlayer(Direction.Right);
        game.MovePlayer(Direction.Down);

        Assert.Equal(new Position(3, 2), game.PlayerPosition);
    }
}