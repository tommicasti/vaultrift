using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Tests.Combat;

public class StatsTests
{
    [Fact]
    public void Constructor_ValidValues_SetsProperties()
    {
        var stats = new Stats(maxHealth: 30, attack: 5, defense: 2, evasion: 10);

        Assert.Equal(30, stats.MaxHealth);
        Assert.Equal(5, stats.Attack);
        Assert.Equal(2, stats.Defense);
        Assert.Equal(10, stats.Evasion);
    }

    [Fact]
    public void Constructor_WithoutEvasion_DefaultsToZero()
    {
        var stats = new Stats(maxHealth: 30, attack: 5, defense: 2);

        Assert.Equal(0, stats.Evasion);
    }

    [Theory]
    [InlineData(0, 5, 2)]
    [InlineData(-1, 5, 2)]
    [InlineData(30, -1, 2)]
    [InlineData(30, 5, -1)]
    public void Constructor_InvalidValues_Throws(int maxHealth, int attack, int defense)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stats(maxHealth, attack, defense));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_EvasionOutOfRange_Throws(int evasion)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stats(maxHealth: 30, attack: 5, defense: 2, evasion: evasion));
    }

    [Fact]
    public void Apply_AddsModifiersToTheMatchingStats()
    {
        var stats = new Stats(maxHealth: 30, attack: 2, defense: 2, evasion: 5);

        var result = stats.Apply(
        [
            new StatModifier(StatType.Attack, 3),
            new StatModifier(StatType.Defense, 1),
            new StatModifier(StatType.Evasion, 5),
            new StatModifier(StatType.Attack, 2)
        ]);

        Assert.Equal(new Stats(maxHealth: 30, attack: 7, defense: 3, evasion: 10), result);
    }

    [Fact]
    public void Apply_WithoutModifiers_ReturnsEqualStats()
    {
        var stats = new Stats(maxHealth: 30, attack: 2, defense: 2, evasion: 5);

        Assert.Equal(stats, stats.Apply([]));
    }

    [Fact]
    public void Apply_EvasionAboveOneHundred_IsClamped()
    {
        var stats = new Stats(maxHealth: 30, attack: 2, defense: 2, evasion: 90);

        var result = stats.Apply([new StatModifier(StatType.Evasion, 30)]);

        Assert.Equal(100, result.Evasion);
    }
}