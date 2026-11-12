using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class PickupTests
{
    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#######"
    ]);

    private static Player NewPlayer() =>
        new(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 2));

    private static Game NewGame(Player player, params FloorItem[] items) =>
        new(Map, player, [], new FixedRandom(), items);

    private static FloorItem ItemAt(int x, ItemDefinition item) => new(new Position(x, 1), item);

    [Fact]
    public void MovingOntoAnItem_PicksItUp()
    {
        var game = NewGame(NewPlayer(), ItemAt(2, TestItems.Sword));

        var result = game.MovePlayer(Direction.Right);

        Assert.Equal(TestItems.Sword, Assert.Single(game.Player.Inventory.Entries).Item);
        Assert.Empty(game.FloorItems);
        Assert.Contains(new ItemPickedUp(TestItems.Sword), result.Events);
    }

    [Fact]
    public void PickingUp_DoesNotCostAnExtraTurn()
    {
        var game = NewGame(NewPlayer(), ItemAt(2, TestItems.Sword));

        var result = game.MovePlayer(Direction.Right);

        Assert.True(result.TurnPassed);
        Assert.Equal(1, game.TurnNumber);
    }

    [Fact]
    public void PickingUpTheSamePotionTwice_Stacks()
    {
        var game = NewGame(NewPlayer(), ItemAt(2, TestItems.HealingPotion), ItemAt(3, TestItems.HealingPotion));

        game.MovePlayer(Direction.Right);
        game.MovePlayer(Direction.Right);

        Assert.Equal(2, Assert.Single(game.Player.Inventory.Entries).Quantity);
    }

    [Fact]
    public void InventoryFull_ItemStaysOnTheFloor()
    {
        var player = NewPlayer();
        for (var i = 0; i < Inventory.DefaultCapacity; i++)
            player.Inventory.TryAdd(TestItems.Sword);

        var game = NewGame(player, ItemAt(2, TestItems.Armor));

        var result = game.MovePlayer(Direction.Right);

        Assert.Equal(new Position(2, 1), game.Player.Position);
        Assert.Equal(TestItems.Armor, game.FloorItems[new Position(2, 1)]);
        Assert.Contains(new InventoryFull(TestItems.Armor), result.Events);
    }

    [Fact]
    public void Constructor_ItemOnAWall_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            NewGame(NewPlayer(), new FloorItem(new Position(0, 0), TestItems.Sword)));
    }

    [Fact]
    public void Constructor_TwoItemsOnTheSameTile_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            NewGame(NewPlayer(), ItemAt(2, TestItems.Sword), ItemAt(2, TestItems.Armor)));
    }
}