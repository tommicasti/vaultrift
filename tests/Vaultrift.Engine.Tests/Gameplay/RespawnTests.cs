using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class RespawnTests
{
    private const double RespawnRoll = 0.0;
    private const double NoRespawnRoll = 0.99;

    private static readonly DungeonMap LongHall = DungeonMap.Parse(
    [
        "##############",
        "#............#",
        "#............#",
        "##############"
    ]);

    private static readonly EnemyKind Rat = new("test-rat", "Rat", new Stats(maxHealth: 6, attack: 1, defense: 0));

    private static readonly EnemyKind Boss =
        new("test-boss", "Boss", new Stats(maxHealth: 40, attack: 1, defense: 0), isBoss: true);

    private static Player NewPlayer() => new(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 0));

    private static Enemy DefeatedEnemy(EnemyKind kind, int x)
    {
        var enemy = new Enemy(1, new Position(x, 1), kind);
        enemy.TakeDamage(enemy.Stats.MaxHealth);
        return enemy;
    }

    [Fact]
    public void ADefeatedMonsterFarAway_CanComeBackAsleepAtFullHealth()
    {
        var rat = DefeatedEnemy(Rat, 12);
        rat.Position = new Position(11, 1);
        var game = new Game(LongHall, NewPlayer(), [rat], new FixedRandom(RespawnRoll, 0.0));

        var result = game.MovePlayer(Direction.Down);

        Assert.Contains(new EnemyRespawned(1), result.Events);
        Assert.False(rat.IsDead);
        Assert.False(rat.IsAwake);
        Assert.Equal(rat.Stats.MaxHealth, rat.Health);
        Assert.Equal(new Position(12, 1), rat.Position);
    }

    [Fact]
    public void ARespawnedMonster_GivesExperienceAgain()
    {
        var rat = DefeatedEnemy(Rat, 12);
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 100, defense: 0));
        var game = new Game(LongHall, player, [rat], new FixedRandom(RespawnRoll, 0.0, 0.99, 0.99));
        game.MovePlayer(Direction.Down);
        player.Position = new Position(11, 1);
        player.Facing = Direction.Right;

        var result = game.Attack();

        Assert.Contains(new ExperienceGained(3), result.Events);
        Assert.True(rat.IsDead);
    }

    [Fact]
    public void AnUnluckyRoll_KeepsTheMonsterDead()
    {
        var rat = DefeatedEnemy(Rat, 12);
        var game = new Game(LongHall, NewPlayer(), [rat], new FixedRandom(NoRespawnRoll));

        var result = game.MovePlayer(Direction.Down);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyRespawned);
        Assert.True(rat.IsDead);
    }

    [Fact]
    public void MonstersNeverRespawnCloseToTheHero()
    {
        var rat = DefeatedEnemy(Rat, 5);
        var game = new Game(LongHall, NewPlayer(), [rat], new FixedRandom());

        var result = game.MovePlayer(Direction.Down);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyRespawned);
        Assert.True(rat.IsDead);
    }

    [Fact]
    public void BossesNeverRespawn()
    {
        var boss = DefeatedEnemy(Boss, 12);
        var game = new Game(LongHall, NewPlayer(), [boss], new FixedRandom());

        var result = game.MovePlayer(Direction.Down);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyRespawned);
        Assert.True(boss.IsDead);
    }
}