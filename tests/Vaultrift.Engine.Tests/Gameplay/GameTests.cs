using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class GameTests
{
    private const double NoCritical = 0.5;

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "######",
        "#....#",
        "#.#..#",
        "######"
    ]);

    private static Player NewPlayer(int x = 1, int y = 1) =>
        new(new Position(x, y), new Stats(maxHealth: 30, attack: 5, defense: 2));

    private static Enemy NewEnemy(int id, int x, int y, int maxHealth = 10) =>
        new(id, new Position(x, y), new Stats(maxHealth: maxHealth, attack: 4, defense: 1));

    private static Game NewGame(Player player, IRandom random, params Enemy[] enemies) =>
        new(Map, player, enemies, random);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(10, 10)]
    public void Constructor_PlayerNotOnFloor_Throws(int x, int y)
    {
        Assert.Throws<ArgumentException>(() => NewGame(NewPlayer(x, y), new FixedRandom()));
    }

    [Fact]
    public void Constructor_EnemyNotOnFloor_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewGame(NewPlayer(), new FixedRandom(), NewEnemy(1, 2, 2)));
    }

    [Fact]
    public void Constructor_EnemyOnPlayerTile_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewGame(NewPlayer(), new FixedRandom(), NewEnemy(1, 1, 1)));
    }

    [Fact]
    public void Constructor_DuplicateEnemyIds_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            NewGame(NewPlayer(), new FixedRandom(), NewEnemy(1, 3, 1), NewEnemy(1, 4, 1)));
    }

    [Fact]
    public void Constructor_NullMap_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Game(null!, NewPlayer(), [], new FixedRandom()));
    }

    [Fact]
    public void MovePlayer_ToFloor_MovesAndPassesTurn()
    {
        var game = NewGame(NewPlayer(), new FixedRandom());

        var result = game.MovePlayer(Direction.Right);

        Assert.True(result.TurnPassed);
        Assert.Equal(new Position(2, 1), game.Player.Position);
        Assert.Equal(Direction.Right, game.Player.Facing);
        Assert.Contains(new PlayerMoved(new Position(2, 1)), result.Events);
    }

    [Theory]
    [InlineData(1, 1, Direction.Up)]
    [InlineData(1, 1, Direction.Left)]
    [InlineData(2, 1, Direction.Down)]
    public void MovePlayer_IntoWall_TurnsWithoutPassingTurn(int startX, int startY, Direction direction)
    {
        var game = NewGame(NewPlayer(startX, startY), new FixedRandom());

        var result = game.MovePlayer(direction);

        Assert.False(result.TurnPassed);
        Assert.Equal(new Position(startX, startY), game.Player.Position);
        Assert.Equal(direction, game.Player.Facing);
        Assert.Contains(new PlayerTurned(direction), result.Events);
    }

    [Fact]
    public void MovePlayer_IntoEnemy_TurnsWithoutMoving()
    {
        var game = NewGame(NewPlayer(), new FixedRandom(), NewEnemy(1, 2, 1));

        var result = game.MovePlayer(Direction.Right);

        Assert.False(result.TurnPassed);
        Assert.Equal(new Position(1, 1), game.Player.Position);
        Assert.Equal(Direction.Right, game.Player.Facing);
    }

    [Fact]
    public void MovePlayer_SeveralMoves_FollowsThePath()
    {
        var game = NewGame(NewPlayer(), new FixedRandom());

        game.MovePlayer(Direction.Right);
        game.MovePlayer(Direction.Right);
        game.MovePlayer(Direction.Down);

        Assert.Equal(new Position(3, 2), game.Player.Position);
    }

    [Fact]
    public void MovePlayer_EndsNextToEnemy_EnemyAttacks()
    {
        var game = NewGame(NewPlayer(), new FixedRandom(NoCritical), NewEnemy(1, 3, 1));

        var result = game.MovePlayer(Direction.Right);

        Assert.Equal(28, game.Player.Health);
        Assert.Contains(new EnemyAttackedPlayer(1, new AttackResult(2, false)), result.Events);
    }

    [Fact]
    public void Attack_EnemyInFront_DealsDamage()
    {
        var enemy = NewEnemy(1, 2, 1);
        var game = NewGame(NewPlayer(), new FixedRandom(NoCritical, NoCritical), enemy);
        game.MovePlayer(Direction.Right);

        var result = game.Attack();

        Assert.True(result.TurnPassed);
        Assert.Equal(6, enemy.Health);
        Assert.Contains(new PlayerAttackedEnemy(1, new AttackResult(4, false)), result.Events);
    }

    [Fact]
    public void Attack_EnemySurvives_EnemyCounterattacks()
    {
        var game = NewGame(NewPlayer(), new FixedRandom(NoCritical, NoCritical), NewEnemy(1, 2, 1));
        game.MovePlayer(Direction.Right);

        var result = game.Attack();

        Assert.Equal(28, game.Player.Health);
        Assert.Contains(new EnemyAttackedPlayer(1, new AttackResult(2, false)), result.Events);
    }

    [Fact]
    public void Attack_LethalDamage_DefeatsEnemyWithoutCounterattack()
    {
        var enemy = NewEnemy(1, 2, 1, maxHealth: 3);
        var game = NewGame(NewPlayer(), new FixedRandom(NoCritical), enemy);
        game.MovePlayer(Direction.Right);

        var result = game.Attack();

        Assert.True(enemy.IsDead);
        Assert.Equal(0, enemy.Health);
        Assert.Equal(30, game.Player.Health);
        Assert.Contains(new EnemyDefeated(1), result.Events);
    }

    [Fact]
    public void Attack_DefeatedEnemy_FreesTheTile()
    {
        var game = NewGame(NewPlayer(), new FixedRandom(NoCritical), NewEnemy(1, 2, 1, maxHealth: 3));
        game.MovePlayer(Direction.Right);
        game.Attack();

        var result = game.MovePlayer(Direction.Right);

        Assert.True(result.TurnPassed);
        Assert.Equal(new Position(2, 1), game.Player.Position);
    }

    [Fact]
    public void Attack_NothingInFront_StillPassesTurn()
    {
        var game = NewGame(NewPlayer(), new FixedRandom());

        var result = game.Attack();

        Assert.True(result.TurnPassed);
        Assert.Contains(new PlayerAttackedNothing(), result.Events);
    }

    [Fact]
    public void Attack_EnemyFarAway_EnemyDoesNotAttack()
    {
        var game = NewGame(NewPlayer(), new FixedRandom(), NewEnemy(1, 4, 1));

        var result = game.Attack();

        Assert.Equal(30, game.Player.Health);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
    }
}