using Vaultrift.Engine.Combat;

namespace Vaultrift.Engine.Tests.Combat;

public class StatsTests
{
    [Fact]
    public void Constructor_ValidValues_SetsProperties()
    {
        var stats = new Stats(maxHealth: 30, attack: 5, defense: 2);

        Assert.Equal(30, stats.MaxHealth);
        Assert.Equal(5, stats.Attack);
        Assert.Equal(2, stats.Defense);
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
}