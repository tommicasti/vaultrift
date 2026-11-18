using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Combat;

public class BlockTests
{
    private static readonly Stats Attacker = new(maxHealth: 10, attack: 8, defense: 0);

    private static readonly DungeonMap Corridor = DungeonMap.Parse(
    [
        "######",
        "#....#",
        "######"
    ]);

    [Fact]
    public void Calculate_RollsTheBlockAfterAFailedDodge()
    {
        var defender = new Stats(maxHealth: 10, attack: 0, defense: 0, evasion: 10, block: 20);
        var calculator = new DamageCalculator(new FixedRandom(0.5, 0.1));

        var result = calculator.Calculate(Attacker, defender);

        Assert.True(result.IsBlocked);
        Assert.False(result.IsDodged);
        Assert.Equal(0, result.Damage);
    }

    [Fact]
    public void Calculate_ADodgedBlow_IsNeverAlsoBlocked()
    {
        var defender = new Stats(maxHealth: 10, attack: 0, defense: 0, evasion: 10, block: 20);
        var calculator = new DamageCalculator(new FixedRandom(0.05));

        var result = calculator.Calculate(Attacker, defender);

        Assert.True(result.IsDodged);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public void Calculate_BlockIsCappedAtThirtyPercent()
    {
        var defender = new Stats(maxHealth: 10, attack: 0, defense: 0, block: 100);
        var calculator = new DamageCalculator(new FixedRandom(0.31, 0.99));

        Assert.False(calculator.Calculate(Attacker, defender).IsBlocked);
    }

    [Fact]
    public void Calculate_WithoutBlock_DoesNotRollForIt()
    {
        var calculator = new DamageCalculator(new FixedRandom(0.99));

        Assert.Equal(8, calculator.Calculate(Attacker, new Stats(maxHealth: 10, attack: 0, defense: 0)).Damage);
    }

    [Fact]
    public void AShield_CanBlockAnEnemyBlow()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 0)) { Facing = Direction.Left };
        player.Give(TestGear.Shield(block: 15));
        var enemy = new Enemy(1, new Position(2, 1), new Stats(maxHealth: 10, attack: 9, defense: 0));
        var game = new Game(Corridor, player, [enemy], new FixedRandom(0.01));

        var result = game.Attack();

        var blow = Assert.Single(result.Events.OfType<EnemyAttackedPlayer>()).Result;
        Assert.True(blow.IsBlocked);
        Assert.Equal(30, player.Health);
    }
}
