using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class ManaTests
{
    private static readonly DungeonMap Room = DungeonMap.Parse(
    [
        "#####",
        "#...#",
        "#...#",
        "#####"
    ]);

    private static readonly PotionDefinition ManaPotion = new("test-mana", "Test Mana Potion", Rarity.Common, ManaRestore: 10);

    private static Player NewPlayer(int maxMana = 10) =>
        new(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 0, maxMana: maxMana));

    private static GearDefinition ManaRing(int mana) =>
        new("test-mana-ring", "Test Mana Ring", Rarity.Rare, EquipmentKind.Ring, [new StatModifier(StatType.Mana, mana)]);

    private static Game NewGame(Player player) => new(Room, player, [], new FixedRandom());

    private static void PassTurns(Game game, int turns)
    {
        for (var turn = 0; turn < turns; turn++)
            game.MovePlayer(turn % 2 == 0 ? Direction.Down : Direction.Up);
    }

    [Fact]
    public void AHero_StartsWithFullMana()
    {
        var player = NewPlayer(maxMana: 15);

        Assert.Equal(15, player.Mana);
        Assert.Equal(15, player.MaxMana);
    }

    [Fact]
    public void ManaGear_RaisesTheMaximum_ButNotTheCurrentMana()
    {
        var player = NewPlayer();

        player.Give(ManaRing(5));

        Assert.Equal(15, player.MaxMana);
        Assert.Equal(10, player.Mana);
    }

    [Fact]
    public void Mana_RegeneratesEveryOtherTurn()
    {
        var player = NewPlayer();
        player.Give(ManaRing(5));
        var game = NewGame(player);

        PassTurns(game, Progression.ManaRegenInterval);

        Assert.Equal(10 + Progression.ManaRegenAmount, player.Mana);
    }

    [Fact]
    public void ManaFlow_RegeneratesMore()
    {
        var player = NewPlayer();
        player.Give(TestGear.Ring(EffectKind.ManaFlow, 2, new StatModifier(StatType.Mana, 10)));
        var game = NewGame(player);

        PassTurns(game, Progression.ManaRegenInterval);

        Assert.Equal(10 + Progression.ManaRegenAmount + 2, player.Mana);
    }

    [Fact]
    public void RemovingManaGear_CapsTheCurrentMana()
    {
        var player = NewPlayer();
        player.Give(ManaRing(5));
        PassTurns(NewGame(player), 10);

        player.Unequip(EquipmentSlot.Ring1);

        Assert.Equal(10, player.Mana);
    }

    [Fact]
    public void SpendingAPointOnMana_RaisesBothMaximumAndCurrent()
    {
        var player = NewPlayer();
        player.GainExperience(20);

        player.SpendPoint(StatPoint.Mana);

        Assert.Equal(10 + Progression.ManaPerPoint, player.MaxMana);
        Assert.Equal(10 + Progression.ManaPerPoint, player.Mana);
    }

    [Fact]
    public void AManaPotion_RestoresMana()
    {
        var player = NewPlayer();
        player.Give(ManaRing(20));
        player.Inventory.TryAdd(ManaPotion);

        var result = player.Use(player.Inventory.Entries[0].Id);

        Assert.Equal(UseResult.Used, result);
        Assert.Equal(20, player.Mana);
    }

    [Fact]
    public void AManaPotion_AtFullMana_IsRejected()
    {
        var player = NewPlayer();
        player.Inventory.TryAdd(ManaPotion);

        Assert.Equal(UseResult.AlreadyAtFullMana, player.Use(player.Inventory.Entries[0].Id));
        Assert.Single(player.Inventory.Entries);
    }

    [Fact]
    public void LevellingUp_RefillsMana()
    {
        var player = NewPlayer();
        player.Give(ManaRing(10));

        player.GainExperience(20);

        Assert.Equal(20, player.Mana);
    }
}
