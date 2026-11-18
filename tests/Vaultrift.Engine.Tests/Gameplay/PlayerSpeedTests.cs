using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class PlayerSpeedTests
{
    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#........#",
        "#........#",
        "##########"
    ]);

    private static readonly CharacterClass Swift = new(
        "test-swift", "Swift", new Stats(maxHealth: 30, attack: 5, defense: 2),
        new WeaponMastery(WeaponType.Dagger, new StatModifier(StatType.Critical, 10)),
        Speed.Fast);

    private static readonly CharacterClass Steady = new(
        "test-steady", "Steady", new Stats(maxHealth: 30, attack: 5, defense: 2),
        new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Defense, 1)));

    private static Game NewGame(CharacterClass characterClass, Enemy enemy, params double[] randomValues) =>
        new(Map, new Player(new Position(1, 1), characterClass), [enemy], new FixedRandom(randomValues));

    private static Enemy NewEnemy(int x, int y) => new(1, new Position(x, y), new Stats(maxHealth: 50, attack: 1, defense: 0));

    [Fact]
    public void FastPlayer_FirstStep_IsFree()
    {
        var enemy = NewEnemy(6, 1);
        var game = NewGame(Swift, enemy);

        var result = game.MovePlayer(Direction.Right);

        Assert.False(result.TurnPassed);
        Assert.Equal(new Position(2, 1), game.Player.Position);
        Assert.Equal(0, game.TurnNumber);
        Assert.False(enemy.IsAwake);
        Assert.Equal(0, game.FreeStepsLeft);
    }

    [Fact]
    public void FastPlayer_SecondStep_EndsTheTurn()
    {
        var enemy = NewEnemy(8, 1);
        var game = NewGame(Swift, enemy);

        game.MovePlayer(Direction.Right);
        var result = game.MovePlayer(Direction.Right);

        Assert.True(result.TurnPassed);
        Assert.Equal(1, game.TurnNumber);
        Assert.Equal(1, game.FreeStepsLeft);
        Assert.Contains(result.Events, gameEvent => gameEvent is EnemyMoved);
    }

    [Fact]
    public void FastPlayer_StepThenAttack_HitsBeforeTheEnemyActs()
    {
        var enemy = NewEnemy(3, 1);
        var game = NewGame(Swift, enemy, 0.99, 0.99);
        game.Player.Facing = Direction.Right;

        var step = game.MovePlayer(Direction.Right);
        var attack = game.Attack();

        Assert.False(step.TurnPassed);
        Assert.Equal(new EnemyWokeUp(1), attack.Events.OfType<EnemyWokeUp>().Single());
        Assert.True(attack.TurnPassed);
        Assert.Contains(attack.Events, gameEvent => gameEvent is PlayerAttackedEnemy);
        Assert.Equal(1, game.FreeStepsLeft);
    }

    [Fact]
    public void FastPlayer_BumpingIntoAWall_DoesNotSpendTheFreeStep()
    {
        var enemy = NewEnemy(8, 3);
        var game = NewGame(Swift, enemy);

        game.MovePlayer(Direction.Up);

        Assert.Equal(1, game.FreeStepsLeft);
    }

    [Fact]
    public void NormalPlayer_EveryStepEndsTheTurn()
    {
        var enemy = NewEnemy(8, 3);
        var game = NewGame(Steady, enemy);

        var result = game.MovePlayer(Direction.Right);

        Assert.True(result.TurnPassed);
        Assert.Equal(0, game.FreeStepsLeft);
    }
}