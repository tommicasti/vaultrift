using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class RegenerationTests
{
    private const double NoCritical = 0.5;

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#######"
    ]);

    private static Player NewPlayer() =>
        new(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 2));

    private static Game DamagedGameAtTurnTwo()
    {
        var enemy = new Enemy(1, new Position(2, 1), new Stats(maxHealth: 3, attack: 4, defense: 1));
        var game = new Game(Map, NewPlayer(), [enemy], new FixedRandom(NoCritical, NoCritical));

        game.Attack();
        game.MovePlayer(Direction.Right);
        game.Attack();

        return game;
    }

    [Fact]
    public void BeforeTheInterval_HealthDoesNotRegenerate()
    {
        var game = DamagedGameAtTurnTwo();

        Assert.Equal(2, game.TurnNumber);
        Assert.Equal(28, game.Player.Health);
    }

    [Fact]
    public void AtTheInterval_HealthRegeneratesByOne()
    {
        var game = DamagedGameAtTurnTwo();

        game.Attack();

        Assert.Equal(3, game.TurnNumber);
        Assert.Equal(29, game.Player.Health);
    }

    [Fact]
    public void TurnsThatDoNotPass_DoNotCount()
    {
        var game = DamagedGameAtTurnTwo();

        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Up);

        Assert.Equal(2, game.TurnNumber);
        Assert.Equal(28, game.Player.Health);
    }

    [Fact]
    public void Regeneration_NeverExceedsMaxHealth()
    {
        var game = new Game(Map, NewPlayer(), [], new FixedRandom());

        game.Attack();
        game.Attack();
        game.Attack();

        Assert.Equal(30, game.Player.Health);
    }
}