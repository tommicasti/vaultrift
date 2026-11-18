using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class PlayerNameTests
{
    private static readonly CharacterClass Thief = new(
        "test-thief", "Thief", new Stats(maxHealth: 20, attack: 3, defense: 1),
        new WeaponMastery(WeaponType.Dagger, new StatModifier(StatType.Critical, 10)));

    private static readonly Position Start = new(1, 1);

    [Fact]
    public void Constructor_WithAName_KeepsIt()
    {
        Assert.Equal("Tommaso", new Player(Start, Thief, "Tommaso").Name);
    }

    [Fact]
    public void Constructor_TrimsTheName()
    {
        Assert.Equal("Tommaso", new Player(Start, Thief, "  Tommaso  ").Name);
    }

    [Fact]
    public void Constructor_WithoutAName_UsesTheClassName()
    {
        Assert.Equal("Thief", new Player(Start, Thief).Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankName_UsesTheClassName(string name)
    {
        Assert.Equal("Thief", new Player(Start, Thief, name).Name);
    }

    [Fact]
    public void Constructor_WithoutAClass_IsCalledHero()
    {
        Assert.Equal(Player.DefaultName, new Player(Start, Thief.BaseStats).Name);
    }

    [Fact]
    public void Constructor_NameAtTheLimit_IsAccepted()
    {
        var name = new string('a', Player.MaxNameLength);

        Assert.Equal(name, new Player(Start, Thief, name).Name);
    }

    [Fact]
    public void Constructor_NameTooLong_Throws()
    {
        var name = new string('a', Player.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => new Player(Start, Thief, name));
    }
}