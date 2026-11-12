using Vaultrift.Engine.Combat;

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
}