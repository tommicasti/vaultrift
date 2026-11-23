using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public TurnResult Discard(int entryId)
    {
        var entry = Player.Inventory.Find(entryId);

        GameEvent outcome = entry switch
        {
            null => new UseRejected(UseResult.ItemNotFound),
            { Mark: ItemMark.Favorite } => new UseRejected(UseResult.ItemIsFavorite),
            _ => Player.Inventory.Remove(entryId) ? new ItemDiscarded(entry.Item) : new UseRejected(UseResult.ItemNotFound)
        };

        return new TurnResult(TurnPassed: false, [outcome]);
    }

    public TurnResult DiscardJunk()
    {
        var junk = Player.Inventory.RemoveJunk();

        GameEvent outcome = junk.Count == 0
            ? new UseRejected(UseResult.NoJunk)
            : new JunkDiscarded(junk.Sum(entry => entry.Quantity));

        return new TurnResult(TurnPassed: false, [outcome]);
    }

    public TurnResult Mark(int entryId, ItemMark mark)
    {
        var entry = Player.Inventory.Find(entryId)
            ?? Player.Equipment.Values.FirstOrDefault(equipped => equipped.Id == entryId);

        if (entry is null)
            return new TurnResult(TurnPassed: false, [new UseRejected(UseResult.ItemNotFound)]);

        if (Player.IsBound(entry) && mark != ItemMark.Favorite)
            return new TurnResult(TurnPassed: false, [new UseRejected(UseResult.ItemIsFavorite)]);

        entry.Mark = mark;
        return new TurnResult(TurnPassed: false, [new ItemMarked(entry.Item, mark)]);
    }
}
