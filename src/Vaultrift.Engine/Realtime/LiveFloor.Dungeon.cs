using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed partial class LiveFloor
{
    private readonly List<LiveChest> _chests = [];
    private readonly HashSet<int> _onStairs = [];
    private bool _leadsDeeper;
    private bool _stairsWereOpen;
    private int _nextChestId = 1;

    public IReadOnlyList<LiveChest> Chests => _chests;

    public Position? Stairs { get; private set; }

    public bool StairsOpen => Stairs is not null && _leadsDeeper && !_monsters.Any(monster => monster.Kind.IsBoss && !monster.IsDead);

    public void PlaceStairs(Position stairs, bool leadsDeeper)
    {
        Stairs = stairs;
        _leadsDeeper = leadsDeeper;
        _stairsWereOpen = StairsOpen;
    }

    public LiveChest AddChest(Position tile, Enemy? mimic = null)
    {
        var chest = new LiveChest(_nextChestId++, tile, mimic);
        _chests.Add(chest);
        return chest;
    }

    private bool OpenChest(LiveHero hero, List<LiveEvent> events)
    {
        var chest = _chests
            .Where(chest => Near(hero, chest.Tile, LiveRules.FixtureReach))
            .MinBy(chest => hero.Position.DistanceTo(new Vector(chest.Tile.X, chest.Tile.Y)));

        if (chest is null)
            return false;

        _chests.Remove(chest);

        if (chest.Mimic is { } mimic)
        {
            var monster = AddMonster(mimic);
            monster.IsAwake = true;
            monster.AttackReadyAt = Time + LiveRules.MonsterWindupSeconds;
            events.Add(new MimicSprang(hero.Id, chest.Id, monster.Id));
            return true;
        }

        var coins = Progression.ChestCoins(FloorNumber);
        hero.Player.GainCoins(coins);
        hero.Touch();
        events.Add(new ChestLooted(hero.Id, chest.Id, coins));

        if (_loot is null)
            return true;

        foreach (var finder in _heroes)
        {
            var context = new LootContext(finder.ClassId, finder.Player.EffectAmount(EffectKind.Luck), FloorNumber);

            if (_loot.RollChest(context) is not { } item || DropSpot(chest.Tile) is not { } spot)
                continue;

            var dropped = new LiveItem(_nextItemId++, spot, item);
            _items.Add(dropped);
            ItemsVersion++;
            events.Add(new LootDropped(dropped.Id, 0));
        }

        return true;
    }

    private void WatchStairs(List<LiveEvent> events)
    {
        var open = StairsOpen;

        if (open && !_stairsWereOpen && Stairs is { } unsealed)
            events.Add(new StairsUnsealed(unsealed));

        _stairsWereOpen = open;

        foreach (var hero in _heroes)
        {
            var onStairs = open && !hero.IsDead && Collision.TileOf(hero.Position) == Stairs;

            if (!onStairs)
                _onStairs.Remove(hero.Id);
            else if (_onStairs.Add(hero.Id))
                events.Add(new HeroAtStairs(hero.Id));
        }
    }
}
