using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class EnemyKindTests
{
    private static readonly Stats Base = new(maxHealth: 10, attack: 5, defense: 2);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankId_Throws(string id)
    {
        Assert.Throws<ArgumentException>(() => new EnemyKind(id, "Rat", Base));
    }

    [Fact]
    public void Constructor_NullStats_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EnemyKind("rat", "Rat", null!));
    }

    [Fact]
    public void NewEnemy_FromKind_UsesScaledStatsAndKeepsRarityAndLevel()
    {
        var kind = new EnemyKind("rat", "Rat", Base);

        var enemy = new Enemy(1, new Position(1, 1), kind, Rarity.Legendary, level: 3);

        Assert.Same(kind, enemy.Kind);
        Assert.Equal(Rarity.Legendary, enemy.Rarity);
        Assert.Equal(3, enemy.Level);
        Assert.Equal(28, enemy.Stats.MaxHealth);
        Assert.Equal(28, enemy.Health);
    }

    [Fact]
    public void NewEnemy_FromStats_IsANormalCommonLevelOneEnemy()
    {
        var enemy = new Enemy(1, new Position(1, 1), Base);

        Assert.Equal(Speed.Normal, enemy.Kind.Speed);
        Assert.False(enemy.Kind.PassesThroughWalls);
        Assert.Equal(Rarity.Common, enemy.Rarity);
        Assert.Equal(1, enemy.Level);
        Assert.Equal(Base, enemy.Stats);
    }

    [Fact]
    public void NewEnemy_NullKind_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Enemy(1, new Position(1, 1), (EnemyKind)null!));
    }
}