using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Tests.Gameplay;

public class EnemyScalingTests
{
    private static readonly Stats Base = new(maxHealth: 10, attack: 5, defense: 2, evasion: 20);

    [Fact]
    public void Scale_CommonLevelOne_KeepsTheBaseStats()
    {
        Assert.Equal(Base, EnemyScaling.Scale(Base, Rarity.Common, level: 1));
    }

    [Theory]
    [InlineData(Rarity.Rare, 13, 7, 3)]
    [InlineData(Rarity.Epic, 16, 8, 3)]
    [InlineData(Rarity.Legendary, 20, 10, 4)]
    public void Scale_Rarity_MultipliesHealthAttackAndDefense(Rarity rarity, int health, int attack, int defense)
    {
        var scaled = EnemyScaling.Scale(Base, rarity, level: 1);

        Assert.Equal(health, scaled.MaxHealth);
        Assert.Equal(attack, scaled.Attack);
        Assert.Equal(defense, scaled.Defense);
    }

    [Fact]
    public void Scale_EachLevel_AddsTenPercent()
    {
        var scaled = EnemyScaling.Scale(Base, Rarity.Common, level: 5);

        Assert.Equal(14, scaled.MaxHealth);
        Assert.Equal(7, scaled.Attack);
        Assert.Equal(3, scaled.Defense);
    }

    [Fact]
    public void Scale_NeverChangesEvasion()
    {
        var scaled = EnemyScaling.Scale(Base, Rarity.Legendary, level: 10);

        Assert.Equal(20, scaled.Evasion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Scale_LevelBelowOne_Throws(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EnemyScaling.Scale(Base, Rarity.Common, level));
    }
}