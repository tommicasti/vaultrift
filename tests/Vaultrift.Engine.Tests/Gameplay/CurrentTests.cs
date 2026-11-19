using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class CurrentTests
{
    private static readonly EnemyKind Dummy = new("test-dummy", "Dummy", new Stats(maxHealth: 40, attack: 1, defense: 0));

    private static readonly EnemyKind Griffin = new("test-griffin", "Griffin", new Stats(maxHealth: 60, attack: 1, defense: 0),
        isBoss: true, isStationary: true, power: BossPower.Gust);

    private static Player NewPlayer(int x = 1, int y = 1) =>
        new(new Position(x, y), new Stats(maxHealth: 40, attack: 5, defense: 0)) { Facing = Direction.Right };

    private static Game NewGame(string[] rows, Player player, params Enemy[] enemies) =>
        new(DungeonMap.Parse(rows), player, enemies, new FixedRandom(Dice.AlwaysFail()));

    [Fact]
    public void Currents_AreWalkable_AndPointSomewhere()
    {
        var map = DungeonMap.Parse(["######", "#^v<>#", "######"]);

        Assert.True(map.IsWalkable(new Position(1, 1)));
        Assert.Equal(Direction.Up, map.CurrentAt(new Position(1, 1)));
        Assert.Equal(Direction.Down, map.CurrentAt(new Position(2, 1)));
        Assert.Equal(Direction.Left, map.CurrentAt(new Position(3, 1)));
        Assert.Equal(Direction.Right, map.CurrentAt(new Position(4, 1)));
    }

    [Fact]
    public void ACurrent_CarriesTheHeroOneTile()
    {
        var player = NewPlayer();
        var game = NewGame(["######", "#.>..#", "######"], player);

        var result = game.MovePlayer(Direction.Right);

        Assert.Contains(new PlayerDrifted(new Position(3, 1)), result.Events);
        Assert.Equal(new Position(3, 1), player.Position);
    }

    [Fact]
    public void ACurrentAgainstAWall_CarriesNobody()
    {
        var player = NewPlayer();
        var game = NewGame(["####", "#.>#", "####"], player);

        var result = game.MovePlayer(Direction.Right);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is PlayerDrifted);
        Assert.Equal(new Position(2, 1), player.Position);
    }

    [Fact]
    public void Monsters_RideTheCurrentsToo()
    {
        var sleeper = new Enemy(1, new Position(10, 1), Dummy);
        var game = NewGame(["############", "#.........v#", "#..........#", "############"], NewPlayer(), sleeper);

        var result = game.MovePlayer(Direction.Down);

        Assert.Contains(new EnemyDrifted(1, new Position(10, 2)), result.Events);
    }

    [Fact]
    public void Bosses_HoldTheirGround()
    {
        var bossKind = new EnemyKind("test-boss", "Boss", new Stats(maxHealth: 60, attack: 1, defense: 0), isBoss: true);
        var boss = new Enemy(1, new Position(10, 1), bossKind);
        var game = NewGame(["############", "#.........v#", "#..........#", "############"], NewPlayer(), boss);

        game.MovePlayer(Direction.Down);

        Assert.Equal(new Position(10, 1), boss.Position);
    }

    [Fact]
    public void Gust_BlowsTheHeroThreeTilesAway()
    {
        var player = NewPlayer(2, 2);
        var griffin = new Enemy(1, new Position(1, 1), Griffin) { IsAwake = true };
        var game = NewGame(["##########", "#........#", "#........#", "##########"], player, griffin);

        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Down);
        var third = game.MovePlayer(Direction.Up);

        Assert.Contains(new PlayerBlownAway(1, new Position(5, 1), 0), third.Events);
        Assert.Equal(new Position(5, 1), player.Position);
    }

    [Fact]
    public void Gust_SlamsTheHeroIntoAWall()
    {
        var player = NewPlayer(2, 2);
        var griffin = new Enemy(1, new Position(1, 1), Griffin) { IsAwake = true };
        var game = NewGame(["######", "#....#", "#....#", "######"], player, griffin);

        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Down);
        var third = game.MovePlayer(Direction.Up);

        Assert.Contains(new PlayerBlownAway(1, new Position(4, 1), Game.LavaDamage(40)), third.Events);
        Assert.Equal(40 - Game.LavaDamage(40), player.Health);
    }
}
