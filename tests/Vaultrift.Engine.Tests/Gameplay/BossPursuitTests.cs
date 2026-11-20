using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class BossPursuitTests
{
    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "############",
        "#..........#",
        "#..........#",
        "############"
    ]);

    private static readonly Stats BossStats = new(maxHealth: 50, attack: 4, defense: 1);

    private static readonly EnemyKind Guardian = new("guardian", "Guardian", BossStats, isBoss: true);
    private static readonly EnemyKind SlowGuardian = new("slow-guardian", "Slow Guardian", BossStats, Speed.Slow, isBoss: true);
    private static readonly EnemyKind Rat = new("rat", "Rat", BossStats);

    private static Game NewGame(Enemy enemy) =>
        new(Map,
            new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 2)),
            [enemy],
            new FixedRandom(0.99));

    private static Enemy At(EnemyKind kind, int x) => new(1, new Position(x, 1), kind);

    [Fact]
    public void AFarBoss_TakesTwoSteps()
    {
        var boss = At(Guardian, 6);
        var game = NewGame(boss);

        game.Attack();

        Assert.Equal(new Position(4, 1), boss.Position);
    }

    [Fact]
    public void ABossThatIsClose_WalksNormally()
    {
        var boss = At(Guardian, 3);
        var game = NewGame(boss);

        game.Attack();

        Assert.Equal(new Position(2, 1), boss.Position);
    }

    [Fact]
    public void ABoss_CannotBeKitedByRunningAway()
    {
        var boss = At(Guardian, 5);
        var game = NewGame(boss);

        game.MovePlayer(Direction.Down);
        game.MovePlayer(Direction.Right);

        Assert.True(Math.Abs(boss.Position.X - game.Player.Position.X) + Math.Abs(boss.Position.Y - game.Player.Position.Y) <= 2);
    }

    [Fact]
    public void ASlowBoss_DoesNotRestWhileThePlayerIsFar()
    {
        var boss = At(SlowGuardian, 6);
        var game = NewGame(boss);

        game.Attack();
        game.Attack();

        Assert.Equal(new Position(2, 1), boss.Position);
    }

    [Fact]
    public void OrdinaryMonsters_DoNotPursue()
    {
        var rat = At(Rat, 5);
        var game = NewGame(rat);

        game.Attack();

        Assert.Equal(new Position(4, 1), rat.Position);
    }
}
