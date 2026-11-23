using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class CrystalTests
{
    private static readonly EnemyKind Dummy = new("test-dummy", "Dummy", new Stats(maxHealth: 40, attack: 1, defense: 0));

    private static readonly EnemyKind Golem = new("test-golem", "Golem", new Stats(maxHealth: 80, attack: 6, defense: 0),
        isBoss: true, isStationary: true, power: BossPower.CrystalBloom);

    private static readonly string[] Room =
    [
        "#########",
        "#.......#",
        "#.......#",
        "#.......#",
        "#########"
    ];

    private static Player NewPlayer(int x = 1, int y = 1) =>
        new(new Position(x, y), new Stats(maxHealth: 40, attack: 5, defense: 0)) { Facing = Direction.Right };

    private static Game NewGame(string[] rows, Player player, params Enemy[] enemies) =>
        new(DungeonMap.Parse(rows), player, enemies, new FixedRandom(Dice.AlwaysFail(80)));

    [Fact]
    public void Crystals_BlockTheWay()
    {
        var player = NewPlayer();
        var game = NewGame(["#####", "#.C.#", "#####"], player);

        var result = game.MovePlayer(Direction.Right);

        Assert.True(game.Map.IsCrystal(new Position(2, 1)));
        Assert.False(game.Map.IsWalkable(new Position(2, 1)));
        Assert.False(result.TurnPassed);
        Assert.Equal(new Position(1, 1), player.Position);
    }

    [Fact]
    public void StrikingACrystal_ShattersIt_AndTheShardsHitEveryMonsterAroundIt()
    {
        var player = NewPlayer();
        var behind = new Enemy(1, new Position(3, 1), Dummy);
        var diagonal = new Enemy(2, new Position(3, 2), Dummy);
        var game = NewGame(["######", "#.C..#", "#....#", "######"], player, behind, diagonal);

        var result = game.Attack();

        Assert.Contains(new CrystalShattered(new Position(2, 1)), result.Events);
        Assert.Contains(new EnemyShardHit(1, 5), result.Events);
        Assert.Contains(new EnemyShardHit(2, 5), result.Events);
        Assert.Equal(35, behind.Health);
        Assert.True(game.Map.IsWalkable(new Position(2, 1)));
    }

    [Fact]
    public void Shards_CanFinishAMonster()
    {
        var player = NewPlayer();
        var weak = new Enemy(1, new Position(3, 1), new Stats(maxHealth: 4, attack: 1, defense: 0));
        var game = NewGame(["######", "#.C..#", "######"], player, weak);

        var result = game.Attack();

        Assert.Contains(new EnemyDefeated(1), result.Events);
    }

    [Fact]
    public void AnArrow_ShattersTheFirstCrystalOnItsPath()
    {
        var player = NewPlayer();
        player.Give(TestGear.Bow);
        var hidden = new Enemy(1, new Position(6, 1), Dummy);
        var game = NewGame(["#########", "#....C..#", "#########"], player, hidden);

        var result = game.Attack();

        Assert.Contains(new CrystalShattered(new Position(5, 1)), result.Events);
        Assert.Equal(1, Assert.Single(result.Events.OfType<EnemyShardHit>()).EnemyId);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is PlayerAttackedNothing);
    }

    [Fact]
    public void AGhostInsideACrystal_IsStruckInsteadOfTheCrystal()
    {
        var ghostKind = new EnemyKind("test-ghost", "Ghost", new Stats(maxHealth: 40, attack: 1, defense: 0), passesThroughWalls: true);
        var ghost = new Enemy(1, new Position(3, 1), ghostKind) { IsAwake = true };
        var player = NewPlayer();
        var game = NewGame(["#####", "#.C.#", "#####"], player, ghost);
        player.Facing = Direction.Up;
        game.Attack();
        player.Facing = Direction.Right;

        var result = game.Attack();

        Assert.Contains(result.Events, gameEvent => gameEvent is PlayerAttackedEnemy { EnemyId: 1 });
        Assert.True(game.Map.IsCrystal(new Position(2, 1)));
    }

    [Fact]
    public void CrystalBloom_TheGolemGrowsCrystalsAroundItself_ThenBurstsThem()
    {
        var player = NewPlayer(3, 2);
        var golem = new Enemy(1, new Position(5, 2), Golem) { IsAwake = true };
        var game = NewGame(Room, player, golem);

        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Down);
        var third = game.MovePlayer(Direction.Up);

        var grown = Assert.Single(third.Events.OfType<CrystalsGrew>());
        Assert.Equal(8, grown.Tiles.Count);
        Assert.All(grown.Tiles, tile => Assert.True(game.Map.IsCrystal(tile)));

        game.MovePlayer(Direction.Down);
        game.MovePlayer(Direction.Up);
        var sixth = game.MovePlayer(Direction.Down);

        Assert.Equal(8, Assert.Single(sixth.Events.OfType<CrystalsBurst>()).Tiles.Count);
        Assert.Contains(new PlayerShardHit(6), sixth.Events);
        Assert.All(grown.Tiles, tile => Assert.False(game.Map.IsCrystal(tile)));
    }

    [Fact]
    public void CrystalBloom_DoesNotHurtAHeroStandingAway()
    {
        var player = NewPlayer(1, 2);
        var golem = new Enemy(1, new Position(5, 2), Golem) { IsAwake = true };
        var game = NewGame(Room, player, golem);
        var events = new List<GameEvent>();

        for (var turn = 0; turn < 6; turn++)
            events.AddRange(game.MovePlayer(turn % 2 == 0 ? Direction.Up : Direction.Down).Events);

        Assert.Single(events.OfType<CrystalsBurst>());
        Assert.DoesNotContain(events, gameEvent => gameEvent is PlayerShardHit);
        Assert.Equal(40, player.Health);
    }

    [Fact]
    public void BreakingTheGolemsCrystals_HurtsTheGolem()
    {
        var player = NewPlayer(3, 2);
        var golem = new Enemy(1, new Position(5, 2), Golem) { IsAwake = true };
        var game = NewGame(Room, player, golem);

        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Down);
        game.MovePlayer(Direction.Up);
        player.Facing = Direction.Right;

        var result = game.Attack();

        Assert.Contains(new CrystalShattered(new Position(4, 1)), result.Events);
        Assert.Contains(new EnemyShardHit(1, 5), result.Events);
    }

    [Fact]
    public void StrikingAnObstacle_SmashesIt_AndOpensThePath()
    {
        var player = NewPlayer();
        var game = NewGame(["#####", "#.o.#", "#####"], player);

        var result = game.Attack();

        Assert.Contains(new ObstacleSmashed(new Position(2, 1)), result.Events);
        Assert.True(result.TurnPassed);
        Assert.True(game.Map.IsWalkable(new Position(2, 1)));
    }

    [Fact]
    public void AMonsterStandingBesideAnObstacle_IsStillTheTarget()
    {
        var player = NewPlayer();
        var enemy = new Enemy(1, new Position(2, 1), Dummy);
        var game = NewGame(["#####", "#...#", "#.o.#", "#####"], player, enemy);

        var result = game.Attack();

        Assert.DoesNotContain(result.Events, e => e is ObstacleSmashed);
        Assert.Equal(TileType.Obstacle, game.Map.GetTile(new Position(2, 2)));
    }
}
