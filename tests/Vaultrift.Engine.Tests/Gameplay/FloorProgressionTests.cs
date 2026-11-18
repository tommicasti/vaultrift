using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class FloorProgressionTests
{
    private static readonly DungeonMap Corridor = DungeonMap.Parse(
    [
        "########",
        "#......#",
        "#......#",
        "########"
    ]);

    private static readonly Position Start = new(1, 1);
    private static readonly Position StairsTile = new(3, 1);

    private static readonly EnemyKind WeakBoss =
        new("test-boss", "Test Boss", new Stats(maxHealth: 1, attack: 1, defense: 0), isBoss: true);

    private static readonly EnemyKind Brute =
        new("test-brute", "Test Brute", new Stats(maxHealth: 50, attack: 100, defense: 0));

    private static Floor FirstFloor() =>
        new(Corridor, Start, [new Enemy(1, new Position(1, 2), WeakBoss)], [], StairsTile);

    private static Floor SecondFloor() =>
        new(Corridor, new Position(6, 2), [new Enemy(2, new Position(1, 1), WeakBoss)], []);

    private static Floor DeadlyFloor() =>
        new(Corridor, new Position(6, 2), [new Enemy(3, new Position(6, 1), Brute), new Enemy(4, new Position(1, 1), WeakBoss)], []);

    private static Game NewGame(params Func<Floor>[] floors) =>
        new(new Player(Start, new Stats(maxHealth: 30, attack: 5, defense: 2)),
            new TestFloors(floors),
            new FixedRandom(Enumerable.Repeat(0.99, 50).ToArray()));

    private static void KillTheBossBelow(Game game)
    {
        game.MovePlayer(Direction.Down);
        game.Attack();
    }

    [Fact]
    public void NewGame_StartsOnTheFirstFloor_WithSealedStairs()
    {
        var game = NewGame(FirstFloor, SecondFloor);

        Assert.Equal(1, game.FloorNumber);
        Assert.Equal(2, game.FloorCount);
        Assert.Equal(StairsTile, game.Stairs);
        Assert.False(game.StairsOpen);
    }

    [Fact]
    public void SteppingOnSealedStairs_DoesNotDescend()
    {
        var game = NewGame(FirstFloor, SecondFloor);

        game.MovePlayer(Direction.Right);
        var result = game.MovePlayer(Direction.Right);

        Assert.Contains(new StairsSealed(), result.Events);
        Assert.Equal(1, game.FloorNumber);
        Assert.Equal(StairsTile, game.Player.Position);
    }

    [Fact]
    public void DefeatingTheBoss_OpensTheStairs()
    {
        var game = NewGame(FirstFloor, SecondFloor);

        game.MovePlayer(Direction.Down);
        var result = game.Attack();

        Assert.Contains(new StairsOpened(StairsTile), result.Events);
        Assert.True(game.StairsOpen);
    }

    [Fact]
    public void SteppingOnOpenStairs_DescendsAndRestoresHealth()
    {
        var game = NewGame(FirstFloor, SecondFloor);
        KillTheBossBelow(game);
        game.Player.TakeDamage(10);

        game.MovePlayer(Direction.Right);
        game.MovePlayer(Direction.Up);
        var result = game.MovePlayer(Direction.Right);

        Assert.True(result.TurnPassed);
        Assert.Contains(new FloorDescended(2, new Position(6, 2)), result.Events);
        Assert.Equal(2, game.FloorNumber);
        Assert.Equal(new Position(6, 2), game.Player.Position);
        Assert.Equal(game.Player.Stats.MaxHealth, game.Player.Health);
        Assert.Null(game.Stairs);
        Assert.Equal(new[] { 2 }, game.Enemies.Select(enemy => enemy.Id));
    }

    [Fact]
    public void DefeatingTheLastBoss_ConquersTheVault()
    {
        var game = NewGame(FirstFloor);

        game.MovePlayer(Direction.Down);
        var result = game.Attack();

        Assert.Contains(new VaultConquered(), result.Events);
        Assert.True(game.IsConquered);
        Assert.False(game.StairsOpen);
    }

    [Fact]
    public void DyingOnALowerFloor_RespawnsAtThatFloorsStart()
    {
        var game = NewGame(FirstFloor, DeadlyFloor);
        KillTheBossBelow(game);
        game.MovePlayer(Direction.Right);
        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Right);

        var result = game.Attack();

        Assert.Contains(new PlayerRespawned(new Position(6, 2)), result.Events);
        Assert.Equal(2, game.FloorNumber);
    }
}