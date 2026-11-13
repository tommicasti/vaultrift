using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class UseItemTests
{
    private const double NoCritical = 0.5;

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#######"
    ]);

    private static Player NewPlayer(int maxHealth = 30) =>
        new(new Position(1, 1), new Stats(maxHealth: maxHealth, attack: 2, defense: 2));

    private static int AddToInventory(Player player, ItemDefinition item, int count = 1)
    {
        for (var i = 0; i < count; i++)
            player.Inventory.TryAdd(item);

        return player.Inventory.Entries.First(entry => entry.Item == item).Id;
    }

    [Fact]
    public void Use_HealingPotion_HealsAndConsumesOne()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion, count: 2);
        player.TakeDamage(15);

        var result = player.Use(potionId);

        Assert.Equal(UseResult.Used, result);
        Assert.Equal(25, player.Health);
        Assert.Equal(1, player.Inventory.Find(potionId)?.Quantity);
    }

    [Fact]
    public void Use_HealingPotion_DoesNotExceedMaxHealth()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);
        player.TakeDamage(3);

        player.Use(potionId);

        Assert.Equal(30, player.Health);
    }

    [Fact]
    public void Use_HealingPotionAtFullHealth_IsRejected()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);

        var result = player.Use(potionId);

        Assert.Equal(UseResult.AlreadyAtFullHealth, result);
        Assert.NotNull(player.Inventory.Find(potionId));
    }

    [Fact]
    public void Use_LastPotion_RemovesTheEntry()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);
        player.TakeDamage(5);

        player.Use(potionId);

        Assert.Empty(player.Inventory.Entries);
    }

    [Fact]
    public void Use_BoostPotion_AddsATemporaryModifier()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.StrengthPotion);

        player.Use(potionId);

        Assert.Equal(5, player.CombatStats.Attack);
        Assert.Equal(10, Assert.Single(player.Boosts).RemainingTurns);
    }

    [Fact]
    public void Use_SameBoostTwice_RefreshesInsteadOfStacking()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.StrengthPotion, count: 2);
        player.Use(potionId);
        player.AdvanceBoosts();
        player.AdvanceBoosts();

        player.Use(potionId);

        Assert.Equal(5, player.CombatStats.Attack);
        Assert.Equal(10, Assert.Single(player.Boosts).RemainingTurns);
    }

    [Fact]
    public void Use_Equipment_IsNotUsable()
    {
        var player = NewPlayer();
        var swordId = AddToInventory(player, TestItems.Sword);

        Assert.Equal(UseResult.NotUsable, player.Use(swordId));
    }

    [Fact]
    public void Use_UnknownEntry_IsRejected()
    {
        var player = NewPlayer();

        Assert.Equal(UseResult.ItemNotFound, player.Use(99));
    }

    [Fact]
    public void AssignQuickSlot_Potion_Succeeds()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);

        var assigned = player.AssignQuickSlot(potionId);

        Assert.True(assigned);
        Assert.Equal(TestItems.HealingPotion, player.QuickSlot);
    }

    [Fact]
    public void AssignQuickSlot_Equipment_Fails()
    {
        var player = NewPlayer();
        var swordId = AddToInventory(player, TestItems.Sword);

        var assigned = player.AssignQuickSlot(swordId);

        Assert.False(assigned);
        Assert.Null(player.QuickSlot);
    }

    [Fact]
    public void UseQuickSlot_UsesTheAssignedPotion()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion, count: 2);
        player.AssignQuickSlot(potionId);
        player.TakeDamage(15);

        var result = player.UseQuickSlot();

        Assert.Equal(UseResult.Used, result);
        Assert.Equal(25, player.Health);
    }

    [Fact]
    public void UseQuickSlot_WhenEmpty_IsRejected()
    {
        var player = NewPlayer();

        Assert.Equal(UseResult.QuickSlotEmpty, player.UseQuickSlot());
    }

    [Fact]
    public void UseQuickSlot_NoPotionsLeft_IsRejectedButKeepsTheSlot()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);
        player.AssignQuickSlot(potionId);
        player.TakeDamage(15);
        player.UseQuickSlot();

        var result = player.UseQuickSlot();

        Assert.Equal(UseResult.ItemNotFound, result);
        Assert.Equal(TestItems.HealingPotion, player.QuickSlot);
    }

    [Fact]
    public void UseQuickSlot_WorksAgainWhenThePotionIsFound()
    {
        var player = NewPlayer();
        var firstId = AddToInventory(player, TestItems.HealingPotion);
        player.AssignQuickSlot(firstId);
        player.TakeDamage(15);
        player.UseQuickSlot();
        player.Inventory.TryAdd(TestItems.HealingPotion);

        var result = player.UseQuickSlot();

        Assert.Equal(UseResult.Used, result);
        Assert.Equal(30, player.Health);
    }

    [Fact]
    public void GameUseItem_PassesTheTurn()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.StrengthPotion);
        var game = new Game(Map, player, [], new FixedRandom());

        var result = game.UseItem(potionId);

        Assert.True(result.TurnPassed);
        Assert.Equal(1, game.TurnNumber);
        Assert.Contains(new ItemUsed(TestItems.StrengthPotion), result.Events);
    }

    [Fact]
    public void GameUseItem_Rejected_DoesNotPassTheTurn()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.HealingPotion);
        var game = new Game(Map, player, [], new FixedRandom());

        var result = game.UseItem(potionId);

        Assert.False(result.TurnPassed);
        Assert.Equal(0, game.TurnNumber);
        Assert.Contains(new UseRejected(UseResult.AlreadyAtFullHealth), result.Events);
    }

    [Fact]
    public void Boost_ExpiresAfterItsTurns()
    {
        var player = NewPlayer();
        var potionId = AddToInventory(player, TestItems.StrengthPotion);
        var game = new Game(Map, player, [], new FixedRandom());
        game.UseItem(potionId);

        for (var turn = 0; turn < 8; turn++)
            game.Attack();

        Assert.Equal(5, game.Player.CombatStats.Attack);

        var result = game.Attack();

        Assert.Equal(2, game.Player.CombatStats.Attack);
        Assert.Empty(game.Player.Boosts);
        Assert.Contains(new BoostExpired(TestItems.StrengthPotion), result.Events);
    }

    [Fact]
    public void Death_ClearsBoosts()
    {
        var player = NewPlayer(maxHealth: 2);
        var potionId = AddToInventory(player, TestItems.StrengthPotion);
        var enemy = new Enemy(1, new Position(2, 1), new Stats(maxHealth: 10, attack: 4, defense: 0));
        var game = new Game(Map, player, [enemy], new FixedRandom(NoCritical));

        game.UseItem(potionId);

        Assert.Equal(1, game.Deaths);
        Assert.Empty(game.Player.Boosts);
    }
}