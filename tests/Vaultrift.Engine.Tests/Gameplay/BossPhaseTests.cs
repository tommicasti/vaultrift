using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class BossPhaseTests
{
    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#........#",
        "#........#",
        "##########"
    ]);

    private static readonly EnemyKind Whelp = new("whelp", "Whelp", new Stats(maxHealth: 3, attack: 1, defense: 0));

    private static readonly EnemyKind Dragon = new(
        "dragon", "Dragon", new Stats(maxHealth: 100, attack: 1, defense: 0), isBoss: true, power: BossPower.LavaRing, minion: Whelp)
    {
        Phases =
        [
            new BossPhase(100, BossPower.LavaRing, Speed.Normal),
            new BossPhase(66, BossPower.SummonMinions, Speed.Normal),
            new BossPhase(33, BossPower.Gust, Speed.Fast)
        ]
    };

    private static Enemy NewDragon(int x = 4, int y = 2) => new(1, new Position(x, y), Dragon);

    [Theory]
    [InlineData(0, 1, BossPower.LavaRing, Speed.Normal)]
    [InlineData(33, 1, BossPower.LavaRing, Speed.Normal)]
    [InlineData(34, 2, BossPower.SummonMinions, Speed.Normal)]
    [InlineData(67, 3, BossPower.Gust, Speed.Fast)]
    [InlineData(99, 3, BossPower.Gust, Speed.Fast)]
    public void ThePhase_FollowsTheHealthLeft(int damage, int phase, BossPower power, Speed speed)
    {
        var dragon = NewDragon();

        dragon.TakeDamage(damage);

        Assert.Equal(phase, dragon.PhaseNumber);
        Assert.Equal(power, dragon.Power);
        Assert.Equal(speed, dragon.Speed);
    }

    [Fact]
    public void AnOrdinaryBoss_HasNoPhases()
    {
        var plain = new Enemy(1, new Position(1, 1), new EnemyKind("plain", "Plain", new Stats(10, 1, 0), isBoss: true, power: BossPower.Gust));

        Assert.Equal(0, plain.PhaseNumber);
        Assert.Equal(BossPower.Gust, plain.Power);
    }

    [Fact]
    public void ANewPhase_IsAnnouncedOnce_AndStartsItsPowerAtOnce()
    {
        var dragon = NewDragon(x: 5);
        var game = new Game(Map, new Player(new Position(1, 2), new Stats(maxHealth: 50, attack: 1, defense: 50)), [dragon], new FixedRandom(0.99));
        dragon.TakeDamage(40);

        var first = game.Attack();
        var second = game.Attack();

        Assert.Contains(new BossPhaseChanged(1, 2, BossPower.SummonMinions), first.Events);
        Assert.Contains(first.Events, gameEvent => gameEvent is EnemySummoned);
        Assert.DoesNotContain(second.Events, gameEvent => gameEvent is BossPhaseChanged);
    }

    [Fact]
    public void TheMinionsWither_WhenTheBossDies_EvenIfItsLastPhaseDoesNotSummon()
    {
        var dragon = NewDragon(x: 5);
        var game = new Game(Map, new Player(new Position(1, 2), new Stats(maxHealth: 50, attack: 200, defense: 50)), [dragon], new FixedRandom(0.99));
        dragon.TakeDamage(40);
        game.Attack();
        dragon.TakeDamage(dragon.Health - 1);
        var minions = game.Enemies.Where(enemy => enemy.IsSummoned).ToList();
        game.Player.Position = new Position(dragon.Position.X - 1, dragon.Position.Y);
        game.Player.Facing = Direction.Right;

        game.Attack();

        Assert.NotEmpty(minions);
        Assert.All(minions, minion => Assert.True(minion.IsDead));
    }
}
