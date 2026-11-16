using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class CharacterClassTests
{
    private static readonly CharacterClass Knight = new(
        "test-knight",
        "Test Knight",
        new Stats(maxHealth: 30, attack: 2, defense: 3),
        new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Defense, 2)));

    private static Player NewKnight() => new(new Position(1, 1), Knight);

    private static void EquipNew(Player player, ItemDefinition item)
    {
        player.Inventory.TryAdd(item);
        player.Equip(player.Inventory.Entries[^1].Id);
    }

    [Fact]
    public void NewPlayerFromClass_UsesItsBaseStats()
    {
        var player = NewKnight();

        Assert.Same(Knight, player.Class);
        Assert.Equal(3, player.Stats.Defense);
        Assert.Equal(30, player.Health);
    }

    [Fact]
    public void Mastery_AppliesWithAWeaponOfItsType()
    {
        var player = NewKnight();

        EquipNew(player, TestItems.Sword);

        Assert.Equal(5, player.CombatStats.Defense);
        Assert.Equal(5, player.CombatStats.Attack);
    }

    [Fact]
    public void Mastery_DoesNotApplyWithOtherWeapons()
    {
        var player = NewKnight();

        EquipNew(player, TestItems.Axe);

        Assert.Equal(3, player.CombatStats.Defense);
        Assert.Equal(4, player.CombatStats.Attack);
    }

    [Fact]
    public void Mastery_DoesNotApplyWithoutAWeapon()
    {
        var player = NewKnight();

        Assert.Equal(3, player.CombatStats.Defense);
    }

    [Fact]
    public void PlayerWithoutAClass_HasNoMastery()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 2, defense: 2));

        EquipNew(player, TestItems.Sword);

        Assert.Null(player.Class);
        Assert.Equal(2, player.CombatStats.Defense);
    }

    [Fact]
    public void Constructor_NullClass_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Player(new Position(1, 1), (CharacterClass)null!));
    }
}