using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed partial class LiveFloor
{
    private readonly List<LiveGrave> _graves = [];
    private readonly HashSet<int> _atMerchant = [];
    private readonly HashSet<int> _atWaystone = [];
    private readonly Dictionary<int, double> _totemPrimed = [];
    private int _nextGraveId = 1;
    private int _nextMonsterId = LiveRules.FirstSpawnedMonsterId;

    public FallRule Falls { get; init; } = FallRule.LoseCoins;

    public Merchant? Merchant { get; private set; }

    public Position? Waystone { get; private set; }

    public LiveTotem? Totem { get; private set; }

    public IReadOnlyList<LiveGrave> Graves => _graves;

    public void PlaceMerchant(Merchant merchant)
    {
        ArgumentNullException.ThrowIfNull(merchant);
        Merchant = merchant;
    }

    public void PlaceWaystone(Position waystone) => Waystone = waystone;

    public void PlaceTotem(Position tile, Enemy boss, int price)
    {
        ArgumentNullException.ThrowIfNull(boss);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        Totem = new LiveTotem(tile, boss, price);
    }

    public LiveGrave AddGrave(Guid owner, string ownerName, Position tile, IReadOnlyList<(EquipmentSlot Slot, InventoryEntry Entry)> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var grave = new LiveGrave(_nextGraveId++, owner, ownerName, tile, items);
        _graves.Add(grave);
        return grave;
    }

    public IReadOnlyList<LiveMonster> Reinforce(int heroes)
    {
        var tier = ReinforcementTier(heroes);
        var eligible = _monsters
            .Where(monster => !monster.IsDead && !monster.Kind.IsBoss && !monster.Kind.IsStationary && monster.SummonerId is null)
            .OrderBy(monster => monster.Id)
            .ToList();
        var count = (int)Math.Round(eligible.Count * LiveRules.ReinforcementShare * tier);
        var added = new List<LiveMonster>();

        for (var index = 0; index < count && eligible.Count > 0; index++)
        {
            var model = eligible[(int)((long)index * eligible.Count / count) % eligible.Count];

            if (ReinforcementSpot(model) is not { } spot)
                continue;

            var source = model.Source;
            added.Add(AddMonster(new Enemy(_nextMonsterId++, spot, source.Kind, source.Rarity, source.Level)));
        }

        return added;
    }

    private Position? ReinforcementSpot(LiveMonster model)
    {
        var home = Collision.TileOf(model.Home);
        var start = Collision.TileOf(Start);

        for (var radius = 1; radius <= 2; radius++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var tile = new Position(home.X + dx, home.Y + dy);

                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius || !Map.IsWalkable(tile) || Map.IsLava(tile) || IsFixture(tile))
                        continue;

                    if (Math.Abs(tile.X - start.X) + Math.Abs(tile.Y - start.Y) < LiveRules.WakeDistance)
                        continue;

                    if (_monsters.Any(monster => !monster.IsDead && Collision.TileOf(monster.Home) == tile))
                        continue;

                    return tile;
                }
            }
        }

        return null;
    }

    public bool IsAtMerchant(int heroId) => _atMerchant.Contains(heroId);

    public bool IsAtWaystone(int heroId) => _atWaystone.Contains(heroId);

    public bool IsAtTotem(int heroId) =>
        Totem is { } totem && HeroById(heroId) is { IsDead: false } hero && Near(hero, totem.Tile, LiveRules.FixtureReach);

    public int SellPrice(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return ItemPrices.SellValue(entry.Item.Definition, FloorNumber);
    }

    public TurnResult Buy(int heroId, int offerId)
    {
        if (TraderAt(heroId) is not { } hero)
            return TradeRejected(TradeResult.NoMerchantNearby);

        if (Merchant!.Find(offerId) is not { } offer)
            return TradeRejected(TradeResult.OfferNotFound);

        if (hero.Player.Coins < offer.Price)
            return TradeRejected(TradeResult.NotEnoughCoins);

        if (!hero.Player.Inventory.TryAdd(offer.Item))
            return TradeRejected(TradeResult.BackpackFull);

        hero.Player.TrySpendCoins(offer.Price);
        Merchant.Sold(offer);
        TouchTraders();
        return Traded(new ItemBought(offer.Item, offer.Price));
    }

    public TurnResult BuyBackpackUpgrade(int heroId)
    {
        if (TraderAt(heroId) is not { } hero)
            return TradeRejected(TradeResult.NoMerchantNearby);

        var inventory = hero.Player.Inventory;

        if (!inventory.CanExpand)
            return TradeRejected(TradeResult.BackpackAtMax);

        var price = Merchant.UpgradePrice(inventory);

        if (!hero.Player.TrySpendCoins(price))
            return TradeRejected(TradeResult.NotEnoughCoins);

        inventory.Expand();
        hero.Touch();
        return Traded(new BackpackExpanded(inventory.Capacity, price));
    }

    public TurnResult Sell(int heroId, int entryId)
    {
        if (TraderAt(heroId) is not { } hero)
            return TradeRejected(TradeResult.NoMerchantNearby);

        if (hero.Player.Inventory.Find(entryId) is not { } entry)
            return TradeRejected(TradeResult.ItemNotFound);

        if (entry.Mark == ItemMark.Favorite)
            return TradeRejected(TradeResult.ItemIsFavorite);

        var price = SellPrice(entry);
        hero.Player.Inventory.RemoveOne(entryId);
        hero.Player.GainCoins(price);
        hero.Touch();
        return Traded(new ItemSold(entry.Item, price));
    }

    public TurnResult SellJunk(int heroId)
    {
        if (TraderAt(heroId) is not { } hero)
            return TradeRejected(TradeResult.NoMerchantNearby);

        var junk = hero.Player.Inventory.RemoveJunk();

        if (junk.Count == 0)
            return TradeRejected(TradeResult.NoJunk);

        var price = junk.Sum(entry => SellPrice(entry) * entry.Quantity);
        hero.Player.GainCoins(price);
        hero.Touch();
        return Traded(new JunkSold(junk.Sum(entry => entry.Quantity), price));
    }

    private LiveHero? HeroById(int heroId) => _heroes.FirstOrDefault(hero => hero.Id == heroId);

    private LiveHero? TraderAt(int heroId) =>
        Merchant is not null && HeroById(heroId) is { IsDead: false } hero && Near(hero, Merchant.Position, LiveRules.FixtureReach) ? hero : null;

    private void TouchTraders()
    {
        foreach (var hero in _heroes)
            hero.Touch();
    }

    private static TurnResult TradeRejected(TradeResult reason) => new(TurnPassed: false, [new TradeRejected(reason)]);

    private static TurnResult Traded(GameEvent outcome) => new(TurnPassed: false, [outcome]);

    private static bool Near(LiveHero hero, Position tile, double reach) =>
        hero.Position.DistanceTo(new Vector(tile.X, tile.Y)) <= reach;

    private IEnumerable<Position> SolidFixtures()
    {
        if (Merchant is { } merchant)
            yield return merchant.Position;

        if (Waystone is { } waystone)
            yield return waystone;

        if (Totem is { } totem)
            yield return totem.Tile;

        foreach (var chest in _chests)
            yield return chest.Tile;
    }

    private bool BlockedByFixture(Vector from, Vector point) =>
        SolidFixtures().Any(tile =>
        {
            var center = new Vector(tile.X, tile.Y);
            var distance = point.DistanceTo(center);
            return distance < LiveRules.FixtureRadius + LiveRules.HeroRadius && distance < from.DistanceTo(center);
        });

    private bool IsFixture(Position tile) => SolidFixtures().Contains(tile) || tile == Stairs || _chests.Any(chest => chest.Tile == tile);

    private void WatchFixtures(List<LiveEvent> events)
    {
        foreach (var hero in _heroes)
        {
            var alive = !hero.IsDead;
            Visit(hero, alive && Merchant is { } merchant && Near(hero, merchant.Position, LiveRules.FixtureReach), _atMerchant);
            Visit(hero, alive && Waystone is { } waystone && Near(hero, waystone, LiveRules.FixtureReach), _atWaystone);

            if (alive)
                RecoverGrave(hero, events);
        }
    }

    private void ArriveQuietly(LiveHero hero)
    {
        if (Waystone is { } waystone && Near(hero, waystone, LiveRules.FixtureReach))
            _atWaystone.Add(hero.Id);
    }

    private static void Visit(LiveHero hero, bool near, HashSet<int> visiting)
    {
        if (near ? visiting.Add(hero.Id) : visiting.Remove(hero.Id))
            hero.Touch();
    }

    private bool Hail(LiveHero hero, List<LiveEvent> events)
    {
        if (Merchant is { } merchant && Near(hero, merchant.Position, LiveRules.FixtureReach))
        {
            _atMerchant.Add(hero.Id);
            events.Add(new LiveMerchantGreeted(hero.Id));
            return true;
        }

        if (Waystone is { } waystone && Near(hero, waystone, LiveRules.FixtureReach))
        {
            _atWaystone.Add(hero.Id);
            events.Add(new LiveWaystoneTouched(hero.Id));
            return true;
        }

        return false;
    }

    private void RecoverGrave(LiveHero hero, List<LiveEvent> events)
    {
        var grave = _graves.FirstOrDefault(grave => grave.Owner == hero.Owner && Near(hero, grave.Tile, LiveRules.PickUpReach + 0.2));

        if (grave is null)
            return;

        var left = hero.Player.Reclaim(grave.Items);
        var recovered = grave.Count - left.Count;

        if (recovered == 0)
            return;

        var index = _graves.IndexOf(grave);

        if (left.Count == 0)
            _graves.RemoveAt(index);
        else
            _graves[index] = grave with { Items = left };

        TouchTraders();
        events.Add(new LiveGraveRecovered(hero.Id, grave.Id, recovered, left.Count));
    }

    private void DigGrave(LiveHero hero, List<LiveEvent> events)
    {
        if (hero.Owner == Guid.Empty)
            return;

        foreach (var old in _graves.Where(grave => grave.Owner == hero.Owner).ToList())
        {
            _graves.Remove(old);
            events.Add(new LiveGraveLost(old.Id, old.Owner));
        }

        var items = hero.Player.StripEquipment();
        hero.Player.TakeSpareWeapon();

        if (items.Count == 0)
            return;

        var grave = AddGrave(hero.Owner, hero.Name, GraveSpot(Collision.TileOf(hero.Position)), items);
        TouchTraders();
        events.Add(new LiveGraveDug(hero.Id, grave.Id, grave.Count));
    }

    private Position GraveSpot(Position origin)
    {
        if (Map.IsWalkable(origin) && !Map.IsLava(origin) && !IsFixture(origin))
            return origin;

        return DropSpot(origin) ?? Collision.TileOf(Start);
    }

    private bool StrikeTotem(LiveHero hero, List<LiveEvent> events)
    {
        if (Totem is not { } totem || !Near(hero, totem.Tile, LiveRules.HeroReach + LiveRules.FixtureRadius))
            return false;

        var canAfford = hero.Player.Coins >= totem.Price;

        if (!canAfford || !_totemPrimed.TryGetValue(hero.Id, out var primedAt) || Time - primedAt > LiveRules.TotemPrimeSeconds)
        {
            if (canAfford)
                _totemPrimed[hero.Id] = Time;
            else
                _totemPrimed.Remove(hero.Id);

            events.Add(new LiveTotemPrimed(hero.Id, totem.Price, canAfford));
            return true;
        }

        _totemPrimed.Clear();
        hero.Player.TrySpendCoins(totem.Price);
        hero.Touch();

        var boss = totem.Boss;
        var awakened = AddMonster(new Enemy(_nextMonsterId++, totem.Tile, boss.Kind, boss.Rarity, boss.Level));
        awakened.IsAwake = true;
        awakened.AttackReadyAt = Time + LiveRules.MonsterWindupSeconds;
        Totem = null;
        _floorCleared = false;
        TouchTraders();
        events.Add(new LiveTotemAwakened(hero.Id, awakened.Id, totem.Price));
        return true;
    }
}
