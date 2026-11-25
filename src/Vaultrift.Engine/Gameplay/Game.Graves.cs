using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public Difficulty Difficulty { get; init; }

    public Grave? Grave { get; private set; }

    public bool IsLost { get; private set; }

    public bool LeavesGraves => Difficulty != Difficulty.Normal;

    public Grave? GraveHere => Grave is { } grave && grave.Floor == FloorNumber ? grave : null;

    private bool HandleDeath(List<GameEvent> events)
    {
        if (Difficulty == Difficulty.Hardcore)
        {
            Deaths++;
            IsLost = true;
            events.Add(new PlayerDefeated());
            events.Add(new HeroLost());
            return true;
        }

        return false;
    }

    private void DigGrave(List<GameEvent> events)
    {
        if (Grave is { } lost)
            events.Add(new GraveLost(lost.Position, lost.Count));

        var items = Player.StripEquipment();
        Grave = items.Count == 0 ? null : new Grave(Player.Position, items, FloorNumber);

        if (Grave is { } grave)
            events.Add(new GraveDug(grave.Position, grave.Count));
    }

    private void TakeSpareWeapon(List<GameEvent> events)
    {
        if (LeavesGraves && Player.TakeSpareWeapon())
            events.Add(new SpareWeaponTaken(Player.Equipment[EquipmentSlot.MainHand].Item));
    }

    private void RecoverGraveAt(Position position, List<GameEvent> events)
    {
        if (GraveHere is not { } grave || grave.Position != position)
            return;

        var left = Player.Reclaim(grave.Items);
        var recovered = grave.Count - left.Count;
        Grave = left.Count == 0 ? null : grave with { Items = left };

        if (recovered > 0)
            events.Add(new GraveRecovered(recovered, left.Count));
    }

    private void LeaveGraveBehind(List<GameEvent>? events)
    {
        if (events is not null && GraveHere is { } grave)
            events.Add(new GraveLeftBehind(grave.Floor, grave.Count));
    }

    private void ReturnToGrave(List<GameEvent>? events)
    {
        if (Grave is not { } grave || grave.Floor != FloorNumber)
            return;

        var spot = FreeTileNear(_playerStart, minimumDistance: 2) ?? _playerStart;
        Grave = grave with { Position = spot };
        events?.Add(new GraveAwaits(spot, grave.Count));
    }

    private Position? FreeTileNear(Position origin, int minimumDistance)
    {
        var seen = new HashSet<Position> { origin };
        var queue = new Queue<(Position Tile, int Steps)>();
        queue.Enqueue((origin, 0));

        while (queue.Count > 0)
        {
            var (tile, steps) = queue.Dequeue();

            if (steps >= minimumDistance && CanHoldDrop(tile) && !IsFixture(tile) && FindLivingEnemyAt(tile) is null)
                return tile;

            foreach (var direction in new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left })
            {
                var next = tile.Move(direction);
                if (Map.IsWalkable(next) && seen.Add(next))
                    queue.Enqueue((next, steps + 1));
            }
        }

        return null;
    }

    private TurnResult Defeated() => new(TurnPassed: false, [new HeroLost()]);
}
