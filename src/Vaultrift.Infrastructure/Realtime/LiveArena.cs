using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.Persistence;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Levels;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Infrastructure.Realtime;

public static class LiveArena
{
    public const int FirstHeroId = 1;
    public const Difficulty CoopDifficulty = Difficulty.Hard;
    public const int LastFloor = VaultFloors.FinalFloor;
    public const int GraveDistance = 2;

    public static LiveFloor Open(WorldSession world) => OpenFloor(world, world.Floor, world.Random);

    public static LiveFloor OpenFloor(WorldSession world, int number, IRandom random, long startTick = 0)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);

        var floor = world.CreateFloor(number);
        var live = new LiveFloor(floor.Map, floor.Start, random, floor.Biome, new VaultLoot(random), number, startTick)
        {
            Falls = FallRuleOf(world.Difficulty),
            IsFinal = number == VaultFloors.FinalFloor
        };
        var awaitsTotem = world.IsCleared(number) && number < VaultFloors.FinalFloor;

        foreach (var enemy in floor.Enemies.Where(enemy => !enemy.IsDead && !enemy.Kind.IsStationary))
        {
            if (awaitsTotem && enemy.Kind.IsBoss)
                live.PlaceTotem(enemy.Position, enemy, Game.TotemPriceFor(number, world.TotemUses));
            else
                live.AddMonster(enemy);
        }

        foreach (var chest in floor.Chests ?? [])
            live.AddChest(chest.Position, chest.Mimic);

        if (floor.Stairs is { } stairs)
            live.PlaceStairs(stairs, LeadsDeeper(world, number));

        if (floor.Merchant is { } merchant)
            live.PlaceMerchant(merchant);

        if ((floor.Waystone ?? WaystoneSpot(floor)) is { } waystone)
            live.PlaceWaystone(waystone);

        foreach (var grave in world.GravesOn(number))
        {
            var tile = FreeTileNear(live, floor.Start, GraveDistance) ?? floor.Start;
            live.AddGrave(grave.Owner, grave.OwnerName, tile, GraveItems.Restore(grave.Items, VaultContent.Instance));
        }

        return live;
    }

    public static FallRule FallRuleOf(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Hard => FallRule.DigGrave,
        Difficulty.Hardcore => FallRule.Permadeath,
        _ => FallRule.LoseCoins
    };

    public static IReadOnlyList<GraveSave> GravesOf(LiveFloor floor) =>
        floor.Graves.Select(grave => new GraveSave(grave.Owner, grave.OwnerName, floor.FloorNumber, GraveItems.Capture(grave.Items))).ToList();

    private static Position? WaystoneSpot(Floor floor)
    {
        foreach (var direction in new[] { Direction.Up, Direction.Right, Direction.Left, Direction.Down })
        {
            var tile = floor.Start.Move(direction);

            if (floor.Map.GetTile(tile) == TileType.Floor && tile != floor.Stairs && floor.Merchant?.Position != tile &&
                (floor.Chests ?? []).All(chest => chest.Position != tile) && floor.Items.All(item => item.Position != tile) &&
                floor.Enemies.All(enemy => enemy.Position != tile))
                return tile;
        }

        return null;
    }

    private static Position? FreeTileNear(LiveFloor floor, Position origin, int minimumDistance)
    {
        var seen = new HashSet<Position> { origin };
        var queue = new Queue<(Position Tile, int Steps)>();
        queue.Enqueue((origin, 0));
        var taken = floor.Graves.Select(grave => grave.Tile)
            .Concat(floor.Chests.Select(chest => chest.Tile))
            .Concat(new[] { floor.Stairs, floor.Waystone, floor.Merchant?.Position, floor.Totem?.Tile }.OfType<Position>())
            .ToHashSet();

        while (queue.Count > 0)
        {
            var (tile, steps) = queue.Dequeue();

            if (steps >= minimumDistance && floor.Map.GetTile(tile) == TileType.Floor && !taken.Contains(tile))
                return tile;

            foreach (var direction in new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left })
            {
                var next = tile.Move(direction);
                if (floor.Map.IsWalkable(next) && seen.Add(next))
                    queue.Enqueue((next, steps + 1));
            }
        }

        return null;
    }

    public static bool LeadsDeeper(WorldSession world, int number)
    {
        ArgumentNullException.ThrowIfNull(world);
        return number < Math.Min(world.FloorCount, LastFloor);
    }

    public static LiveHero Enter(LiveFloor floor, int heroId, HeroSession hero)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(hero);

        return floor.AddHero(heroId, hero.Player, hero.Id);
    }

    public static JoinRefusal? Refusal(WorldSession world, bool shared)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Difficulty == Difficulty.Hardcore)
            return shared ? JoinRefusal.Hardcore : null;

        return shared && world.Difficulty != CoopDifficulty ? JoinRefusal.NotHard : null;
    }
}

public enum JoinRefusal
{
    Hardcore,
    NotHard
}
