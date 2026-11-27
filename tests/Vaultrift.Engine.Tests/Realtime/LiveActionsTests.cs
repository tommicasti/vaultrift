using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveActionsTests
{
    private static Player NewPlayer() => new(new Position(1, 1), new Stats(maxHealth: 30, attack: 2, defense: 0, critical: 0), "Casti");

    private static int Add(Player player, ItemDefinition item)
    {
        player.Inventory.TryAdd(new ItemInstance(item));
        return player.Inventory.Entries.Last(entry => entry.Item.Definition == item).Id;
    }

    [Fact]
    public void EquippingNeverSpendsATurn()
    {
        var player = NewPlayer();
        var entry = Add(player, TestItems.Sword);

        var result = LiveActions.Equip(player, entry);

        Assert.False(result.TurnPassed);
        Assert.IsType<ItemEquipped>(Assert.Single(result.Events));
        Assert.Equal(TestItems.Sword, player.Weapon?.Definition);
    }

    [Fact]
    public void UnequippingReturnsTheItemToTheBackpack()
    {
        var player = NewPlayer();
        LiveActions.Equip(player, Add(player, TestItems.Sword));

        var result = LiveActions.Unequip(player, EquipmentSlot.MainHand);

        Assert.IsType<ItemUnequipped>(Assert.Single(result.Events));
        Assert.Null(player.Weapon);
    }

    [Fact]
    public void PotionsFromTheQuickSlotHealAtOnce()
    {
        var player = NewPlayer();
        var entry = Add(player, TestItems.HealingPotion);
        player.AssignQuickSlot(entry, 0);
        player.TakeDamage(10);

        var result = LiveActions.UseQuickSlot(player, 0);

        Assert.IsType<ItemUsed>(Assert.Single(result.Events));
        Assert.True(player.Health > 20);
    }

    [Fact]
    public void RejectionsAreReported()
    {
        var player = NewPlayer();
        var entry = Add(player, TestItems.HealingPotion);

        var result = LiveActions.Use(player, entry);

        Assert.Equal(UseResult.AlreadyAtFullHealth, Assert.IsType<UseRejected>(Assert.Single(result.Events)).Reason);
    }
}
