using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Support;

namespace Vaultrift.Engine.Tests.Items;

public class InventoryTests
{
    [Fact]
    public void NewInventory_IsEmptyWithDefaultCapacity()
    {
        var inventory = new Inventory();

        Assert.Empty(inventory.Entries);
        Assert.Equal(Inventory.DefaultCapacity, inventory.Capacity);
        Assert.False(inventory.IsFull);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidCapacity_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Inventory(capacity));
    }

    [Fact]
    public void TryAdd_Equipment_TakesANewSlotEachTime()
    {
        var inventory = new Inventory();

        inventory.TryAdd(TestItems.Sword);
        inventory.TryAdd(TestItems.Sword);

        Assert.Equal(2, inventory.Entries.Count);
        Assert.All(inventory.Entries, entry => Assert.Equal(1, entry.Quantity));
        Assert.NotEqual(inventory.Entries[0].Id, inventory.Entries[1].Id);
    }

    [Fact]
    public void TryAdd_SamePotion_StacksInOneSlot()
    {
        var inventory = new Inventory();

        inventory.TryAdd(TestItems.HealingPotion);
        inventory.TryAdd(TestItems.HealingPotion);
        inventory.TryAdd(TestItems.HealingPotion);

        var entry = Assert.Single(inventory.Entries);
        Assert.Equal(3, entry.Quantity);
    }

    [Fact]
    public void TryAdd_DifferentPotions_UseSeparateSlots()
    {
        var inventory = new Inventory();

        inventory.TryAdd(TestItems.HealingPotion);
        inventory.TryAdd(TestItems.StrengthPotion);

        Assert.Equal(2, inventory.Entries.Count);
    }

    [Fact]
    public void TryAdd_WhenFull_RejectsANewSlot()
    {
        var inventory = new Inventory(capacity: 2);
        inventory.TryAdd(TestItems.Sword);
        inventory.TryAdd(TestItems.Armor);

        var added = inventory.TryAdd(TestItems.Sword);

        Assert.False(added);
        Assert.Equal(2, inventory.Entries.Count);
    }

    [Fact]
    public void TryAdd_WhenFull_StillStacksOnAnExistingPotion()
    {
        var inventory = new Inventory(capacity: 1);
        inventory.TryAdd(TestItems.HealingPotion);

        var added = inventory.TryAdd(TestItems.HealingPotion);

        Assert.True(added);
        Assert.Equal(2, Assert.Single(inventory.Entries).Quantity);
    }

    [Fact]
    public void RemoveOne_FromAStack_DecreasesTheQuantity()
    {
        var inventory = new Inventory();
        inventory.TryAdd(TestItems.HealingPotion);
        inventory.TryAdd(TestItems.HealingPotion);
        var entryId = inventory.Entries[0].Id;

        inventory.RemoveOne(entryId);

        Assert.Equal(1, Assert.Single(inventory.Entries).Quantity);
    }

    [Fact]
    public void RemoveOne_LastUnit_FreesTheSlot()
    {
        var inventory = new Inventory(capacity: 1);
        inventory.TryAdd(TestItems.Sword);

        inventory.RemoveOne(inventory.Entries[0].Id);

        Assert.Empty(inventory.Entries);
        Assert.False(inventory.IsFull);
    }

    [Fact]
    public void RemoveOne_UnknownEntry_Throws()
    {
        var inventory = new Inventory();

        Assert.Throws<KeyNotFoundException>(() => inventory.RemoveOne(99));
    }

    [Fact]
    public void EntryIds_AreNeverReused()
    {
        var inventory = new Inventory();
        inventory.TryAdd(TestItems.Sword);
        var firstId = inventory.Entries[0].Id;
        inventory.RemoveOne(firstId);

        inventory.TryAdd(TestItems.Sword);

        Assert.NotEqual(firstId, inventory.Entries[0].Id);
    }
}