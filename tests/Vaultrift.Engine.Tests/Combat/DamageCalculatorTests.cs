using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Tests.Fakes;

namespace Vaultrift.Engine.Tests.Combat;

public class DamageCalculatorTests
{
    private const double NoCritical = 0.5;
    private const double Critical = 0.05;

    private static Stats Attacker(int attack) => new(maxHealth: 30, attack: attack, defense: 0);

    private static Stats Defender(int defense, int evasion = 0) =>
        new(maxHealth: 30, attack: 0, defense: defense, evasion: evasion);

    [Theory]
    [InlineData(8, 3, 5)]
    [InlineData(10, 0, 10)]
    [InlineData(5, 5, 1)]
    [InlineData(2, 10, 1)]
    public void Calculate_WithoutCritical_IsAttackMinusDefenseWithMinimum(int attack, int defense, int expected)
    {
        var calculator = new DamageCalculator(new FixedRandom(NoCritical));

        var result = calculator.Calculate(Attacker(attack), Defender(defense));

        Assert.Equal(expected, result.Damage);
        Assert.False(result.IsCritical);
    }

    [Theory]
    [InlineData(0.0, true, 10)]
    [InlineData(0.099, true, 10)]
    [InlineData(0.10, false, 5)]
    [InlineData(0.99, false, 5)]
    public void Calculate_CriticalDependsOnRandomValue(double roll, bool expectedCritical, int expectedDamage)
    {
        var calculator = new DamageCalculator(new FixedRandom(roll));

        var result = calculator.Calculate(Attacker(8), Defender(3));

        Assert.Equal(expectedCritical, result.IsCritical);
        Assert.Equal(expectedDamage, result.Damage);
    }

    [Fact]
    public void Calculate_Critical_DoublesMinimumDamage()
    {
        var calculator = new DamageCalculator(new FixedRandom(Critical));

        var result = calculator.Calculate(Attacker(2), Defender(10));

        Assert.Equal(2, result.Damage);
        Assert.True(result.IsCritical);
    }

    [Fact]
    public void Calculate_DefenderWithoutEvasion_DoesNotRollForDodge()
    {
        var calculator = new DamageCalculator(new FixedRandom(NoCritical));

        var result = calculator.Calculate(Attacker(8), Defender(3, evasion: 0));

        Assert.False(result.IsDodged);
        Assert.Equal(5, result.Damage);
    }

    [Theory]
    [InlineData(0.0, true, 0)]
    [InlineData(0.199, true, 0)]
    [InlineData(0.20, false, 5)]
    [InlineData(0.99, false, 5)]
    public void Calculate_DodgeDependsOnRandomValue(double roll, bool expectedDodged, int expectedDamage)
    {
        var calculator = new DamageCalculator(new FixedRandom(roll, NoCritical));

        var result = calculator.Calculate(Attacker(8), Defender(3, evasion: 20));

        Assert.Equal(expectedDodged, result.IsDodged);
        Assert.Equal(expectedDamage, result.Damage);
    }

    [Fact]
    public void Calculate_DodgedAttack_IsNeverCritical()
    {
        var calculator = new DamageCalculator(new FixedRandom(0.0, Critical));

        var result = calculator.Calculate(Attacker(8), Defender(3, evasion: 20));

        Assert.True(result.IsDodged);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public void Calculate_EvasionAboveCap_CountsAsFifty()
    {
        var calculator = new DamageCalculator(new FixedRandom(0.6, NoCritical));

        var result = calculator.Calculate(Attacker(8), Defender(3, evasion: 90));

        Assert.False(result.IsDodged);
        Assert.Equal(5, result.Damage);
    }

    [Fact]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DamageCalculator(null!));
    }
}