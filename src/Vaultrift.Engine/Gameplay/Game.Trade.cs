using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public Merchant? Merchant { get; private set; }

    public bool IsNextToMerchant => Merchant is { } merchant && Distance(merchant.Position, Player.Position) == 1;

    public TurnResult Buy(int offerId)
    {
        if (!IsNextToMerchant)
            return Rejected(TradeResult.NoMerchantNearby);

        if (Merchant!.Find(offerId) is not { } offer)
            return Rejected(TradeResult.OfferNotFound);

        if (Player.Coins < offer.Price)
            return Rejected(TradeResult.NotEnoughCoins);

        if (!Player.Inventory.TryAdd(offer.Item))
            return Rejected(TradeResult.BackpackFull);

        Player.TrySpendCoins(offer.Price);
        Merchant.Sold(offer);
        return Traded(new ItemBought(offer.Item, offer.Price));
    }

    public TurnResult BuyBackpackUpgrade()
    {
        if (!IsNextToMerchant)
            return Rejected(TradeResult.NoMerchantNearby);

        if (!Player.Inventory.CanExpand)
            return Rejected(TradeResult.BackpackAtMax);

        var price = Merchant.UpgradePrice(Player.Inventory);

        if (!Player.TrySpendCoins(price))
            return Rejected(TradeResult.NotEnoughCoins);

        Player.Inventory.Expand();
        return Traded(new BackpackExpanded(Player.Inventory.Capacity, price));
    }

    public TurnResult Sell(int entryId)
    {
        if (!IsNextToMerchant)
            return Rejected(TradeResult.NoMerchantNearby);

        if (Player.Inventory.Find(entryId) is not { } entry)
            return Rejected(TradeResult.ItemNotFound);

        if (entry.Mark == ItemMark.Favorite)
            return Rejected(TradeResult.ItemIsFavorite);

        var price = SellPrice(entry);
        Player.Inventory.RemoveOne(entryId);
        Player.GainCoins(price);
        return Traded(new ItemSold(entry.Item, price));
    }

    public TurnResult SellJunk()
    {
        if (!IsNextToMerchant)
            return Rejected(TradeResult.NoMerchantNearby);

        var junk = Player.Inventory.RemoveJunk();

        if (junk.Count == 0)
            return Rejected(TradeResult.NoJunk);

        var price = junk.Sum(entry => SellPrice(entry) * entry.Quantity);
        Player.GainCoins(price);
        return Traded(new JunkSold(junk.Sum(entry => entry.Quantity), price));
    }

    public int SellPrice(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return ItemPrices.SellValue(entry.Item.Definition, FloorNumber);
    }

    private static TurnResult Rejected(TradeResult reason) =>
        new(TurnPassed: false, [new TradeRejected(reason)]);

    private static TurnResult Traded(GameEvent outcome) =>
        new(TurnPassed: false, [outcome]);

    private bool IsMerchant(Position position) => Merchant?.Position == position;

    private void EarnCoins(int amount, List<GameEvent> events)
    {
        if (amount <= 0)
            return;

        Player.GainCoins(amount);
        events.Add(new CoinsGained(amount));
    }

    private void LoseCoins(List<GameEvent> events)
    {
        var lost = Player.Coins * Progression.CoinLossPercent / 100;

        if (lost <= 0)
            return;

        Player.TrySpendCoins(lost);
        events.Add(new CoinsLost(lost));
    }
}
