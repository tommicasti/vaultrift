using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class DeathTests
{
    private const double NoCritical = 0.5;

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#.....#",
        "#######"
    ]);

    private static Player NewFragilePlayer(int x = 1, int y = 1) =>
        new(new Position(x, y), new Stats(maxHealth: 2, attack: 5, defense: 0));

    private static Enemy NewEnemy(int id, int x, int y, int maxHealth = 10) =>
        new(id, new Position(x, y), new Stats(maxHealth: maxHealth, attack: 4, defense: 1));

    private static Game NewGame(Player player, FixedRandom random, params Enemy[] enemies) =>
        new(Map, player, enemies, random);

    [Fact]
    public void PlayerDies_RespawnsAtStartWithFullHealthFacingDown()
    {
        var game = NewGame(NewFragilePlayer(), new FixedRandom(NoCritical), NewEnemy(1, 3, 1));

        var result = game.MovePlayer(Direction.Right);

        Assert.False(game.Player.IsDead);
        Assert.Equal(2, game.Player.Health);
        Assert.Equal(new Position(1, 1), game.Player.Position);
        Assert.Equal(Direction.Down, game.Player.Facing);
        Assert.Equal(1, game.Deaths);
        Assert.Contains(new PlayerDefeated(), result.Events);
        Assert.Contains(new PlayerRespawned(new Position(1, 1)), result.Events);
    }

    [Fact]
    public void PlayerDies_SurvivingEnemiesReturnToStartAsleepWithFullHealth()
    {
        var enemy = NewEnemy(1, 3, 1);
        var game = NewGame(NewFragilePlayer(), new FixedRandom(NoCritical, NoCritical), enemy);
        game.Attack();
        game.MovePlayer(Direction.Right);

        game.Attack();

        Assert.Equal(new Position(3, 1), enemy.Position);
        Assert.False(enemy.IsAwake);
        Assert.Equal(10, enemy.Health);
    }

    [Fact]
    public void PlayerDies_DefeatedEnemiesStayDefeated()
    {
        var defeated = NewEnemy(1, 2, 1, maxHealth: 3);
        var killer = NewEnemy(2, 1, 2);
        var game = NewGame(NewFragilePlayer(), new FixedRandom(NoCritical, NoCritical), defeated, killer);
        game.MovePlayer(Direction.Right);

        var result = game.Attack();

        Assert.True(defeated.IsDead);
        Assert.Equal(0, defeated.Health);
        Assert.Contains(new PlayerRespawned(new Position(1, 1)), result.Events);
    }

    [Fact]
    public void PlayerDies_EachDeathIncrementsTheCount()
    {
        var game = NewGame(NewFragilePlayer(), new FixedRandom(NoCritical, NoCritical), NewEnemy(1, 3, 1));
        game.MovePlayer(Direction.Right);

        game.MovePlayer(Direction.Right);

        Assert.Equal(2, game.Deaths);
    }

    [Fact]
    public void PlayerDies_RemainingEnemiesDoNotAttackInThatTurn()
    {
        var game = NewGame(NewFragilePlayer(2, 1), new FixedRandom(NoCritical), NewEnemy(1, 1, 1), NewEnemy(2, 3, 1));

        var result = game.Attack();

        Assert.Equal(1, game.Deaths);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyAttackedPlayer { EnemyId: 2 });
    }
}