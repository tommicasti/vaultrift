using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class BackpackTests
{
    private static (Game Game, Player Player) NewGame(params ItemDefinition[] items)
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 0));

        foreach (var item in items)
            player.Inventory.TryAdd(new ItemInstance(item));

        var game = new Game(DungeonMap.Parse(["####", "#..#", "####"]), player, [], new FixedRandom(Dice.AlwaysFail()));
        return (game, player);
    }

    private static int EntryOf(Player player, ItemDefinition item) =>
        player.Inventory.Entries.First(entry => entry.Item.Definition == item).Id;

    [Fact]
    public void Marking_TagsTheEntry_WithoutPassingTheTurn()
    {
        var (game, player) = NewGame(TestItems.Sword);
        var entry = EntryOf(player, TestItems.Sword);

        var result = game.Mark(entry, ItemMark.Favorite);

        Assert.False(result.TurnPassed);
        Assert.Equal(ItemMark.Favorite, player.Inventory.Find(entry)!.Mark);
        Assert.Single(result.Events.OfType<ItemMarked>());
    }

    [Fact]
    public void AFavorite_CannotBeThrownAway()
    {
        var (game, player) = NewGame(TestItems.Sword);
        var entry = EntryOf(player, TestItems.Sword);
        game.Mark(entry, ItemMark.Favorite);

        var result = game.Discard(entry);

        Assert.Contains(new UseRejected(UseResult.ItemIsFavorite), result.Events);
        Assert.NotNull(player.Inventory.Find(entry));
    }

    [Fact]
    public void DiscardJunk_ThrowsAwayEveryJunkEntry_AndKeepsTheRest()
    {
        var (game, player) = NewGame(TestItems.Sword, TestItems.Axe, TestItems.Ring, TestItems.HealingPotion, TestItems.HealingPotion);
        game.Mark(EntryOf(player, TestItems.Sword), ItemMark.Junk);
        game.Mark(EntryOf(player, TestItems.HealingPotion), ItemMark.Junk);
        game.Mark(EntryOf(player, TestItems.Ring), ItemMark.Favorite);

        var result = game.DiscardJunk();

        Assert.Contains(new JunkDiscarded(3), result.Events);
        Assert.Equal(
            new List<string> { TestItems.Axe.Id, TestItems.Ring.Id },
            player.Inventory.Entries.Select(entry => entry.Item.Definition.Id).ToList());
    }

    [Fact]
    public void DiscardJunk_WithNothingMarked_IsRejected()
    {
        var (game, _) = NewGame(TestItems.Sword);

        Assert.Contains(new UseRejected(UseResult.NoJunk), game.DiscardJunk().Events);
    }

    [Fact]
    public void AMark_StaysWithTheItem_WhenItIsEquippedAndTakenOff()
    {
        var (game, player) = NewGame(TestItems.Sword);
        var entry = EntryOf(player, TestItems.Sword);
        game.Mark(entry, ItemMark.Favorite);

        game.Equip(entry);
        game.Unequip(EquipmentSlot.MainHand);

        Assert.Equal(ItemMark.Favorite, player.Inventory.Find(entry)!.Mark);
    }

    [Fact]
    public void AnEquippedItem_CanBeMarkedToo()
    {
        var (game, player) = NewGame(TestItems.Sword);
        var entry = EntryOf(player, TestItems.Sword);
        game.Equip(entry);

        game.Mark(entry, ItemMark.Favorite);

        Assert.Equal(ItemMark.Favorite, player.Equipment[EquipmentSlot.MainHand].Mark);
    }

    [Fact]
    public void MarkingAMissingEntry_IsRejected()
    {
        var (game, _) = NewGame();

        Assert.Contains(new UseRejected(UseResult.ItemNotFound), game.Mark(99, ItemMark.Junk).Events);
    }
}
