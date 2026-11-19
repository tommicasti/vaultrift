using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Merchant
{
    public const int UpgradeBasePrice = 100;

    private readonly List<Offer> _offers;

    public Merchant(Position position, IEnumerable<ItemInstance> stock, int floor = 1)
    {
        ArgumentNullException.ThrowIfNull(stock);

        Position = position;
        _offers = stock.Select((item, index) => new Offer(index + 1, item, ItemPrices.BuyPrice(item.Definition, floor))).ToList();
    }

    public Position Position { get; }

    public IReadOnlyList<Offer> Offers => _offers;

    public static int UpgradePrice(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return UpgradeBasePrice * (inventory.Expansions + 1);
    }

    internal Offer? Find(int offerId) => _offers.FirstOrDefault(offer => offer.Id == offerId);

    internal void Remove(Offer offer) => _offers.Remove(offer);
}

public sealed record Offer(int Id, ItemInstance Item, int Price);
