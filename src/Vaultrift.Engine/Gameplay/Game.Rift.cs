using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int TotemBasePrice = 100;
    public const int TotemMarkupPercent = 50;

    private readonly SortedSet<int> _clearedFloors = [];

    public int DeepestFloor { get; private set; }

    public IReadOnlyCollection<int> ClearedFloors => _clearedFloors;

    public Position? Waystone { get; private set; }

    public BossTotem? Totem { get; private set; }

    public int TotemUses { get; private set; }

    public bool IsNextToWaystone => Waystone is { } waystone && Distance(waystone, Player.Position) == 1;

    public bool IsNextToTotem => Totem is { } totem && Distance(totem.Position, Player.Position) == 1;

    public int TotemPrice => TotemPriceFor(FloorNumber, TotemUses);

    public IReadOnlyList<int> TravelDestinations =>
        Enumerable.Range(1, DeepestFloor).Where(floor => floor != FloorNumber).ToList();

    public static int TotemPriceFor(int floor, int uses)
    {
        var price = TotemBasePrice * ItemPrices.FloorFactor(floor);

        for (var use = 0; use < uses; use++)
            price *= (100 + TotemMarkupPercent) / 100.0;

        return (int)Math.Ceiling(price);
    }

    public TurnResult Travel(int floor)
    {
        if (IsLost)
            return Defeated();

        if (!IsNextToWaystone)
            return new TurnResult(TurnPassed: false, [new TravelRejected(TravelResult.NoWaystoneNearby)]);

        if (floor == FloorNumber || floor < 1 || floor > DeepestFloor)
            return new TurnResult(TurnPassed: false, [new TravelRejected(TravelResult.UnknownFloor)]);

        var events = new List<GameEvent>();
        _stepsThisTurn = 0;
        LoadFloor(floor, events);
        Player.Facing = Direction.Down;
        events.Add(new Traveled(FloorNumber, Player.Position));
        return new TurnResult(TurnPassed: true, events);
    }

    public TurnResult AwakenTotem()
    {
        if (IsLost)
            return Defeated();

        if (Totem is not { } totem || !IsNextToTotem)
            return new TurnResult(TurnPassed: false, [new TotemRejected(TotemResult.NoTotemNearby)]);

        var price = TotemPrice;

        if (!Player.TrySpendCoins(price))
            return new TurnResult(TurnPassed: false, [new TotemRejected(TotemResult.NotEnoughCoins)]);

        var boss = totem.Boss;
        var awakened = new Enemy(NextEnemyId(), totem.Position, boss.Kind, boss.Rarity, boss.Level) { IsAwake = true };

        Totem = null;
        TotemUses++;
        _enemies.Add(awakened);
        _bossDefeatAnnounced = true;

        var events = new List<GameEvent> { new TotemAwakened(awakened.Id, price) };
        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    private void PrepareFloor(Position? waystone)
    {
        Waystone = waystone;
        DeepestFloor = Math.Max(DeepestFloor, FloorNumber);
        Totem = null;

        if (_clearedFloors.Contains(FloorNumber) && FloorNumber < FloorCount && _enemies.FirstOrDefault(enemy => enemy.Kind.IsBoss) is { } boss)
        {
            _enemies.RemoveAll(enemy => enemy.Kind.IsBoss);
            Totem = new BossTotem(boss.StartPosition, boss);
            _bossDefeatAnnounced = true;
        }

    }

    private Position? PlaceWaystone()
    {
        foreach (var direction in new[] { Direction.Up, Direction.Right, Direction.Left, Direction.Down })
        {
            var tile = _playerStart.Move(direction);

            if (Map.IsWalkable(tile) && !IsChest(tile) && !IsMerchant(tile) && tile != Stairs &&
                !_floorItems.ContainsKey(tile) && FindLivingEnemyAt(tile) is null && Totem?.Position != tile)
                return tile;
        }

        return null;
    }

    private void MarkFloorCleared() => _clearedFloors.Add(FloorNumber);

    private bool IsFixture(Position position) => position == Waystone || position == Totem?.Position;
}
