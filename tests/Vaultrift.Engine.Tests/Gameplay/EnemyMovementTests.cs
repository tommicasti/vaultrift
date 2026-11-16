using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class EnemyMovementTests
{
    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#...#....#",
        "#........#",
        "##########"
    ]);

    private static readonly Stats EnemyStats = new(maxHealth: 10, attack: 4, defense: 1);

    private static readonly EnemyKind Slime = new("slime", "Slime", EnemyStats, Speed.Slow);
    private static readonly EnemyKind Bat = new("bat", "Bat", EnemyStats, Speed.Fast);
    private static readonly EnemyKind Ghost = new("ghost", "Ghost", EnemyStats, passesThroughWalls: true);
    private static readonly EnemyKind Rat = new("rat", "Rat", EnemyStats);

    private static Game NewGame(int playerX, int playerY, Enemy enemy, params double[] randomValues) =>
        new(Map,
            new Player(new Position(playerX, playerY), new Stats(maxHealth: 30, attack: 5, defense: 2)),
            [enemy],
            new FixedRandom(randomValues));

    private static Enemy NewEnemy(EnemyKind kind, int x, int y) => new(1, new Position(x, y), kind);

    private static TurnResult PassTurn(Game game) => game.Attack();

    [Fact]
    public void SlowEnemy_ActsEveryOtherTurn()
    {
        var slime = NewEnemy(Slime, 5, 1);
        var game = NewGame(1, 1, slime);

        PassTurn(game);
        Assert.Equal(new Position(4, 1), slime.Position);

        var resting = PassTurn(game);
        Assert.Equal(new Position(4, 1), slime.Position);
        Assert.DoesNotContain(resting.Events, gameEvent => gameEvent is EnemyMoved);

        PassTurn(game);
        Assert.Equal(new Position(3, 1), slime.Position);
    }

    [Fact]
    public void SlowEnemy_NextToThePlayer_DoesNotAttackWhileResting()
    {
        var slime = NewEnemy(Slime, 2, 1);
        var game = NewGame(1, 1, slime, 0.99);

        var first = PassTurn(game);
        var second = PassTurn(game);

        Assert.Single(first.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
        Assert.DoesNotContain(second.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
    }

    [Fact]
    public void SlowEnemy_AfterPlayerRespawn_IsNoLongerResting()
    {
        var slime = NewEnemy(Slime, 5, 1);
        var game = NewGame(1, 1, slime);
        PassTurn(game);

        game.Player.TakeDamage(game.Player.Health);
        PassTurn(game);

        Assert.False(slime.IsResting);
    }

    [Fact]
    public void FastEnemy_TakesTwoStepsPerTurn()
    {
        var bat = NewEnemy(Bat, 6, 1);
        var game = NewGame(1, 1, bat);

        var result = PassTurn(game);

        Assert.Equal(new Position(4, 1), bat.Position);
        Assert.Equal(2, result.Events.Count(gameEvent => gameEvent is EnemyMoved));
    }

    [Fact]
    public void FastEnemy_ReachingThePlayer_StopsWithoutAttacking()
    {
        var bat = NewEnemy(Bat, 3, 1);
        var game = NewGame(1, 1, bat);

        var result = PassTurn(game);

        Assert.Equal(new Position(2, 1), bat.Position);
        Assert.Single(result.Events, gameEvent => gameEvent is EnemyMoved);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
    }

    [Fact]
    public void FastEnemy_NextToThePlayer_AttacksOnlyOnce()
    {
        var bat = NewEnemy(Bat, 2, 1);
        var game = NewGame(1, 1, bat, 0.99, 0.99);

        var result = PassTurn(game);

        Assert.Single(result.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
        Assert.Equal(new Position(2, 1), bat.Position);
    }

    [Fact]
    public void Ghost_MovesThroughWalls()
    {
        var ghost = NewEnemy(Ghost, 5, 2);
        var game = NewGame(2, 2, ghost);

        PassTurn(game);

        Assert.Equal(new Position(4, 2), ghost.Position);
    }

    [Fact]
    public void NormalEnemy_SameSituation_IsStoppedByTheWall()
    {
        var rat = NewEnemy(Rat, 5, 2);
        var game = NewGame(2, 2, rat);

        PassTurn(game);

        Assert.Equal(new Position(5, 2), rat.Position);
    }
}