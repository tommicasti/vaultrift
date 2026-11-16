using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class EquipmentTests
{
    private const double NoCritical = 0.5;

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#######"
    ]);

    private static Player NewPlayer() =>
        new(new Position(1, 1), new Stats(maxHealth: 30, attack: 2, defense: 2));

    private static Enemy NewEnemy() =>
        new(1, new Position(2, 1), new Stats(maxHealth: 10, attack: 4, defense: 1));

    private static GearDefinition Gear(EquipmentKind kind) =>
        new($"test-{kind}", $"Test {kind}", Rarity.Common, kind, [new StatModifier(StatType.Defense, 1)]);

    private static int AddToInventory(Player player, ItemDefinition item)
    {
        player.Inventory.TryAdd(item);
        return player.Inventory.Entries[^1].Id;
    }

    [Fact]
    public void Equip_Weapon_AddsItsModifiersToCombatStats()
    {
        var player = NewPlayer();
        var swordId = AddToInventory(player, TestItems.Sword);

        var result = player.Equip(swordId);

        Assert.Equal(EquipResult.Equipped, result);
        Assert.Equal(5, player.CombatStats.Attack);
        Assert.Equal(2, player.Stats.Attack);
    }

    [Theory]
    [InlineData(EquipmentKind.Head, EquipmentSlot.Head)]
    [InlineData(EquipmentKind.Body, EquipmentSlot.Body)]
    [InlineData(EquipmentKind.Feet, EquipmentSlot.Feet)]
    public void Equip_Gear_GoesToTheSlotOfItsKind(EquipmentKind kind, EquipmentSlot expectedSlot)
    {
        var player = NewPlayer();
        var entryId = AddToInventory(player, Gear(kind));

        player.Equip(entryId);

        Assert.Equal(entryId, player.Equipment[expectedSlot].Id);
    }

    [Fact]
    public void Equip_SecondWeapon_ReplacesTheFirst()
    {
        var player = NewPlayer();
        var swordId = AddToInventory(player, TestItems.Sword);
        var axeId = AddToInventory(player, TestItems.Axe);
        player.Equip(swordId);

        player.Equip(axeId);

        Assert.Equal(axeId, player.Equipment[EquipmentSlot.Weapon].Id);
        Assert.Equal(4, player.CombatStats.Attack);
    }

    [Fact]
    public void Equip_FirstRing_GoesToTheFirstRingSlot()
    {
        var player = NewPlayer();
        var ringId = AddToInventory(player, TestItems.Ring);

        player.Equip(ringId);

        Assert.Equal(ringId, player.Equipment[EquipmentSlot.Ring1].Id);
        Assert.False(player.Equipment.ContainsKey(EquipmentSlot.Ring2));
    }

    [Fact]
    public void Equip_SecondRing_GoesToTheSecondRingSlot()
    {
        var player = NewPlayer();
        var firstId = AddToInventory(player, TestItems.Ring);
        var secondId = AddToInventory(player, TestItems.Ring);
        player.Equip(firstId);

        player.Equip(secondId);

        Assert.Equal(firstId, player.Equipment[EquipmentSlot.Ring1].Id);
        Assert.Equal(secondId, player.Equipment[EquipmentSlot.Ring2].Id);
    }

    [Fact]
    public void Equip_ThirdRing_ReplacesTheFirstRingSlot()
    {
        var player = NewPlayer();
        var firstId = AddToInventory(player, TestItems.Ring);
        var secondId = AddToInventory(player, TestItems.Ring);
        var thirdId = AddToInventory(player, TestItems.Ring);
        player.Equip(firstId);
        player.Equip(secondId);

        player.Equip(thirdId);

        Assert.Equal(thirdId, player.Equipment[EquipmentSlot.Ring1].Id);
        Assert.Equal(secondId, player.Equipment[EquipmentSlot.Ring2].Id);
    }

    [Fact]
    public void Equip_Potion_IsRejected()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);

        var result = player.Equip(potionId);

        Assert.Equal(EquipResult.NotEquippable, result);
        Assert.Empty(player.Equipment);
    }

    [Fact]
    public void Equip_UnknownEntry_IsRejected()
    {
        var player = NewPlayer();

        Assert.Equal(EquipResult.ItemNotFound, player.Equip(99));
    }

    [Fact]
    public void Equip_AlreadyEquippedItem_IsRejected()
    {
        var player = NewPlayer();
        var swordId = AddToInventory(player, TestItems.Sword);
        player.Equip(swordId);

        Assert.Equal(EquipResult.AlreadyEquipped, player.Equip(swordId));
    }

    [Fact]
    public void AllEquippedItems_AddTheirModifiers()
    {
        var player = NewPlayer();
        player.Equip(AddToInventory(player, TestItems.Sword));
        player.Equip(AddToInventory(player, TestItems.Armor));
        player.Equip(AddToInventory(player, TestItems.Ring));

        var stats = player.CombatStats;

        Assert.Equal(6, stats.Attack);
        Assert.Equal(3, stats.Defense);
    }

    [Fact]
    public void GameEquip_PassesTheTurn()
    {
        var player = NewPlayer();
        var swordId = AddToInventory(player, TestItems.Sword);
        var game = new Game(Map, player, [], new FixedRandom());

        var result = game.Equip(swordId);

        Assert.True(result.TurnPassed);
        Assert.Equal(1, game.TurnNumber);
        Assert.Contains(new ItemEquipped(TestItems.Sword), result.Events);
    }

    [Fact]
    public void GameEquip_Rejected_DoesNotPassTheTurn()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);
        var game = new Game(Map, player, [], new FixedRandom());

        var result = game.Equip(potionId);

        Assert.False(result.TurnPassed);
        Assert.Equal(0, game.TurnNumber);
        Assert.Contains(new EquipRejected(EquipResult.NotEquippable), result.Events);
    }

    [Fact]
    public void EquippedWeapon_IncreasesDamageDealt()
    {
        var player = NewPlayer();
        var swordId = AddToInventory(player, TestItems.Sword);
        var game = new Game(Map, player, [NewEnemy()], new FixedRandom(NoCritical, NoCritical, NoCritical));
        game.MovePlayer(Direction.Right);
        game.Equip(swordId);

        var result = game.Attack();

        Assert.Contains(new PlayerAttackedEnemy(1, new AttackResult(4, false)), result.Events);
    }

    [Fact]
    public void EquippedArmor_ReducesDamageTaken()
    {
        var player = NewPlayer();
        var armorId = AddToInventory(player, TestItems.Armor);
        var game = new Game(Map, player, [NewEnemy()], new FixedRandom(NoCritical));

        var result = game.Equip(armorId);

        Assert.Equal(29, game.Player.Health);
        Assert.Contains(new EnemyAttackedPlayer(1, new AttackResult(1, false)), result.Events);
    }
}