using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class EnemyBehaviorTests
{
    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#...#....#",
        "#........#",
        "##########"
    ]);

    private static Game NewGame(int playerX, int playerY, params Enemy[] enemies) =>
        new(Map,
            new Player(new Position(playerX, playerY), new Stats(maxHealth: 30, attack: 5, defense: 2)),
            enemies,
            new FixedRandom());

    private static Enemy NewEnemy(int id, int x, int y) =>
        new(id, new Position(x, y), new Stats(maxHealth: 10, attack: 4, defense: 1));

    private static TurnResult PassTurn(Game game) => game.Attack();

    [Fact]
    public void Enemy_AtWakeDistance_WakesAndMovesInTheSameTurn()
    {
        var enemy = NewEnemy(1, 6, 1);
        var game = NewGame(1, 1, enemy);

        var result = PassTurn(game);

        Assert.True(enemy.IsAwake);
        Assert.Equal(new Position(5, 1), enemy.Position);
        Assert.Contains(new EnemyWokeUp(1), result.Events);
        Assert.Contains(new EnemyMoved(1, new Position(5, 1)), result.Events);
    }

    [Fact]
    public void Enemy_BeyondWakeDistance_StaysAsleep()
    {
        var enemy = NewEnemy(1, 7, 1);
        var game = NewGame(1, 1, enemy);

        PassTurn(game);

        Assert.False(enemy.IsAwake);
        Assert.Equal(new Position(7, 1), enemy.Position);
    }

    [Theory]
    [InlineData(4, 3, 3, 3)]
    [InlineData(2, 3, 2, 2)]
    [InlineData(3, 3, 2, 3)]
    public void Enemy_StepsAlongTheLongerAxis_PreferringHorizontalOnTies(
        int enemyX, int enemyY, int expectedX, int expectedY)
    {
        var enemy = NewEnemy(1, enemyX, enemyY);
        var game = NewGame(1, 1, enemy);

        PassTurn(game);

        Assert.Equal(new Position(expectedX, expectedY), enemy.Position);
    }

    [Fact]
    public void Enemy_PreferredStepBlockedByWall_TriesTheOtherAxis()
    {
        var enemy = NewEnemy(1, 5, 2);
        var game = NewGame(2, 1, enemy);

        PassTurn(game);

        Assert.Equal(new Position(5, 1), enemy.Position);
    }

    [Fact]
    public void Enemy_NoStepGetsCloser_StaysStill()
    {
        var enemy = NewEnemy(1, 3, 2);
        var game = NewGame(8, 2, enemy);

        var result = PassTurn(game);

        Assert.True(enemy.IsAwake);
        Assert.Equal(new Position(3, 2), enemy.Position);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyMoved);
    }

    [Fact]
    public void Enemy_Awake_KeepsChasingBeyondWakeDistance()
    {
        var enemy = NewEnemy(1, 3, 2);
        var game = NewGame(8, 2, enemy);
        PassTurn(game);

        game.MovePlayer(Direction.Up);

        Assert.True(enemy.IsAwake);
        Assert.Equal(new Position(3, 1), enemy.Position);
    }

    [Fact]
    public void Enemies_ActInOrder_LaterEnemyUsesTheFreedTile()
    {
        var first = NewEnemy(1, 3, 1);
        var second = NewEnemy(2, 4, 1);
        var game = NewGame(1, 1, first, second);

        PassTurn(game);

        Assert.Equal(new Position(2, 1), first.Position);
        Assert.Equal(new Position(3, 1), second.Position);
    }

    [Fact]
    public void Enemy_BlockedByAnotherEnemy_StaysStill()
    {
        var behind = NewEnemy(2, 4, 1);
        var ahead = NewEnemy(1, 3, 1);
        var game = NewGame(1, 1, behind, ahead);

        PassTurn(game);

        Assert.Equal(new Position(4, 1), behind.Position);
        Assert.Equal(new Position(2, 1), ahead.Position);
    }

    [Fact]
    public void MovePlayer_TurnNotPassed_EnemiesDoNotWake()
    {
        var enemy = NewEnemy(1, 3, 1);
        var game = NewGame(1, 1, enemy);

        game.MovePlayer(Direction.Up);

        Assert.False(enemy.IsAwake);
        Assert.Equal(new Position(3, 1), enemy.Position);
    }
}