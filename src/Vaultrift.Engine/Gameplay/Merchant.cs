using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Merchant
{
    public const int UpgradeBasePrice = 100;
    public const int RestockMarkupPercent = 25;

    private readonly List<Offer> _offers;

    public Merchant(Position position, IEnumerable<ItemInstance> stock, int floor = 1)
        : this(position, stock.Select(item => new StockItem(item)), floor)
    {
    }

    public Merchant(Position position, IEnumerable<StockItem> stock, int floor = 1)
    {
        ArgumentNullException.ThrowIfNull(stock);

        Position = position;
        _offers = stock
            .Select((item, index) => new Offer(index + 1, item.Item, ItemPrices.BuyPrice(item.Item.Definition, floor), item.Kind))
            .ToList();
    }

    private Merchant(Position position, List<Offer> offers)
    {
        Position = position;
        _offers = offers;
    }

    public Position Position { get; }

    internal static Merchant Restore(Position position, IEnumerable<Offer> offers) => new(position, offers.ToList());

    public IReadOnlyList<Offer> Offers => _offers;

    public static int UpgradePrice(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return UpgradeBasePrice * (inventory.Expansions + 1);
    }

    internal Offer? Find(int offerId) => _offers.FirstOrDefault(offer => offer.Id == offerId);

    internal void Sold(Offer offer)
    {
        var index = _offers.IndexOf(offer);

        if (offer.Kind == OfferKind.Unlimited)
            _offers[index] = offer with { Price = Restocked(offer.Price) };
        else
            _offers.RemoveAt(index);
    }

    public static int Restocked(int price) =>
        (int)Math.Ceiling(price * (100 + RestockMarkupPercent) / 100.0);
}

public sealed record Offer(int Id, ItemInstance Item, int Price, OfferKind Kind = OfferKind.Regular);

public sealed record StockItem(ItemInstance Item, OfferKind Kind = OfferKind.Regular);

public enum OfferKind
{
    Regular,
    Featured,
    Unlimited
}
