using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Levels;

public sealed class VaultFloors : IFloorProvider
{
    public const int RolledFloors = 20;
    public const int FinalFloor = RolledFloors + 1;
    public const int FloorCount = FinalFloor;
    public const int FloorsPerBossTier = 5;

    private const int IdsPerFloor = 1000;
    private const int GearPerFloor = 3;
    private const int PotionsPerFloor = 3;
    private const int ScrollsPerFloor = 1;
    private const int MinChestsPerFloor = 2;
    private const int MaxChestsPerFloor = 3;
    private const int ChestPlacementAttempts = 30;
    private const string MimicId = "mimic";
    private const int MerchantEvery = 4;
    private const double MerchantChance = 0.10;
    private const double MerchantChancePerFloor = 0.02;
    private const int FeaturedLegendaryFloor = 10;
    private const int MerchantGear = 3;
    private const int MerchantRarityBoost = 5;

    private static readonly string[] Potions =
        ["potion-healing", "potion-healing", "potion-greater-healing", "potion-mana", "potion-strength", "potion-protection"];

    private readonly IRandom _random;
    private readonly string? _classId;

    public VaultFloors(IRandom random, string? classId = null, Difficulty difficulty = Difficulty.Normal)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
        _classId = classId;
        Difficulty = difficulty;
        Biomes = RollBiomes(BiomeCatalog.All);
    }

    public VaultFloors(IRandom random, string? classId, IReadOnlyList<string> biomeIds, Difficulty difficulty = Difficulty.Normal)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(biomeIds);

        if (biomeIds.Count != RolledFloors)
            throw new ArgumentException($"Expected {RolledFloors} biomes, got {biomeIds.Count}.", nameof(biomeIds));

        _random = random;
        _classId = classId;
        Difficulty = difficulty;
        Biomes = biomeIds.Select(BiomeCatalog.Get).ToList();
    }

    public const int HardLevelBonus = 2;

    public Difficulty Difficulty { get; }

    public int LevelBonus => Difficulty == Difficulty.Normal ? 0 : HardLevelBonus;

    public string? ClassId => _classId;

    public IReadOnlyList<Biome> Biomes { get; }

    public int Count => FloorCount;

    public static Rarity BossRarity(int floor) => ((floor - 1) / FloorsPerBossTier) switch
    {
        0 => Rarity.Common,
        1 => Rarity.Rare,
        _ => Rarity.Epic
    };

    public static int MaxMonstersPerRoom(int floor) => Math.Min(2 + (floor - 1) / 3, 5);

    public static double MimicChance(int floor) => Math.Min(0.4, 0.05 + 0.03 * (floor - 1));

    public static double MonsterRarityChance(Rarity rarity, int floor) => rarity switch
    {
        Rarity.Legendary => Math.Min(0.08, 0.005 * (floor - 1)),
        Rarity.Epic => Math.Min(0.18, 0.03 + 0.01 * (floor - 1)),
        Rarity.Rare => Math.Min(0.35, 0.15 + 0.015 * (floor - 1)),
        _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Common is whatever is left.")
    };

    public static double GearRarityChance(Rarity rarity, int floor) => rarity switch
    {
        Rarity.Legendary => floor < VaultLoot.FirstLegendaryFloor ? 0.0 : Math.Min(0.05, 0.005 * (floor - VaultLoot.FirstLegendaryFloor + 1)),
        Rarity.Epic => Math.Min(0.20, 0.02 + 0.01 * (floor - 1)),
        Rarity.Rare => Math.Min(0.35, 0.20 + 0.01 * (floor - 1)),
        _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Common is whatever is left.")
    };

    public Floor Create(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, Count);

        if (number == FinalFloor)
            return RiftArena.Create(number * IdsPerFloor, LevelBonus);

        var biome = Biomes[number - 1];
        var layout = FloorGenerator.Generate(_random, obstacleDensity: biome.ObstacleDensity, terrain: biome.Terrain);
        var map = layout.Map;
        var start = layout.StartRoom.Center;
        var stairs = number < Count ? layout.BossRoom.Center : (Position?)null;
        var occupied = new HashSet<Position> { start };
        var nextId = number * IdsPerFloor;

        if (stairs is { } stairsPosition)
            occupied.Add(stairsPosition);

        var boss = new Enemy(
            nextId++,
            BossPosition(layout.BossRoom, occupied),
            BestiaryCatalog.Get(biome.BossId),
            BossRarity(number),
            number + LevelBonus);
        var enemies = new List<Enemy> { boss };
        var chests = PlaceChests(layout, number, occupied, () => nextId++);
        var merchant = HasMerchant(number) ? PlaceMerchant(layout, number, occupied, chests) : null;

        foreach (var room in layout.Rooms.Where(room => room != layout.StartRoom && room != layout.BossRoom))
        {
            var count = _random.Next(1, MaxMonstersPerRoom(number) + 1);

            for (var index = 0; index < count && FreeTile(map, room, occupied) is { } position; index++)
            {
                occupied.Add(position);
                var kind = BestiaryCatalog.Get(_random.Pick(biome.MonsterIds));
                enemies.Add(new Enemy(nextId++, position, kind, RollMonsterRarity(number), number + LevelBonus));
            }
        }

        var lootRooms = layout.Rooms.Where(room => room != layout.BossRoom).ToList();
        var items = new List<FloorItem>();

        for (var index = 0; index < GearPerFloor + PotionsPerFloor + ScrollsPerFloor; index++)
        {
            var definition = index < GearPerFloor ? RollGear(number)
                : index < GearPerFloor + PotionsPerFloor ? ItemCatalog.Get(_random.Pick(Potions))
                : _random.Pick(ItemCatalog.Scrolls);

            if (FreeTile(map, _random.Pick(lootRooms), occupied) is not { } position)
                continue;

            occupied.Add(position);
            items.Add(new FloorItem(position, ItemRoller.Roll(definition, _random)));
        }

        return new Floor(map, start, enemies, items, stairs, biome.Id, chests, merchant);
    }

    public static bool AlwaysHasMerchant(int floor) => floor % MerchantEvery == 0 && floor < FloorCount;

    private bool HasMerchant(int floor) =>
        AlwaysHasMerchant(floor) || (floor < FloorCount && _random.NextDouble() < MerchantChanceAt(floor));

    public static double MerchantChanceAt(int floor) => MerchantChance + MerchantChancePerFloor * floor;

    private Merchant? PlaceMerchant(FloorLayout layout, int floor, HashSet<Position> occupied, List<Chest> chests)
    {
        var blocked = chests.Select(chest => chest.Position).ToHashSet();
        var rooms = layout.Rooms.Where(room => room != layout.StartRoom && room != layout.BossRoom).ToList();

        for (var attempt = 0; attempt < ChestPlacementAttempts && rooms.Count > 0; attempt++)
        {
            var room = _random.Pick(rooms);
            var candidates = room.Tiles()
                .Where(tile => tile.X > room.X && tile.X < room.X + room.Width - 1)
                .Where(tile => tile.Y > room.Y && tile.Y < room.Y + room.Height - 1)
                .Where(tile => layout.Map.IsWalkable(tile) && layout.Map.GetTile(tile) == TileType.Floor && !occupied.Contains(tile))
                .ToList();

            if (candidates.Count == 0)
                continue;

            var tile = _random.Pick(candidates);
            blocked.Add(tile);

            if (!EverythingReachable(layout, blocked))
            {
                blocked.Remove(tile);
                continue;
            }

            occupied.Add(tile);
            return new Merchant(tile, RollStock(floor), floor);
        }

        return null;
    }

    private List<StockItem> RollStock(int floor)
    {
        var stock = new List<StockItem>();

        if (RollFeatured(floor) is { } featured)
            stock.Add(new StockItem(featured, OfferKind.Featured));

        for (var index = 0; index < MerchantGear; index++)
            stock.Add(new StockItem(ItemRoller.Roll(RollGear(floor + MerchantRarityBoost), _random)));

        var mods = ItemCatalog.ClassMods.Where(mod => mod.ClassId == _classId && mod.Rarity == ClassModRarity(floor)).ToList();

        if (mods.Count > 0)
            stock.Add(new StockItem(new ItemInstance(_random.Pick(mods))));

        stock.Add(new StockItem(new ItemInstance(_random.Pick(ItemCatalog.Scrolls))));
        stock.Add(new StockItem(new ItemInstance(_random.Pick(ItemCatalog.Runes))));
        stock.Add(new StockItem(new ItemInstance(ItemCatalog.Get("potion-greater-healing")), OfferKind.Unlimited));
        stock.Add(new StockItem(new ItemInstance(ItemCatalog.Get("potion-greater-mana")), OfferKind.Unlimited));
        return stock;
    }

    public static Rarity FeaturedRarity(int floor) => floor < FeaturedLegendaryFloor ? Rarity.Epic : Rarity.Legendary;

    private ItemInstance? RollFeatured(int floor)
    {
        if (_classId is null || ClassCatalog.Find(_classId) is not { } definition)
            return null;

        var rarity = FeaturedRarity(floor);
        var weapons = ItemCatalog.Equipment
            .OfType<WeaponDefinition>()
            .Where(weapon => weapon.WeaponType == definition.Class.Mastery.WeaponType && weapon.Rarity == rarity)
            .ToList();

        return weapons.Count == 0 ? null : ItemRoller.Roll(_random.Pick(weapons), _random);
    }

    private static Rarity ClassModRarity(int floor) => floor switch
    {
        < 8 => Rarity.Rare,
        < 15 => Rarity.Epic,
        _ => Rarity.Legendary
    };

    private List<Chest> PlaceChests(FloorLayout layout, int floor, HashSet<Position> occupied, Func<int> nextId)
    {
        var rooms = layout.Rooms.Where(room => room != layout.StartRoom && room != layout.BossRoom).ToList();
        var wanted = _random.Next(MinChestsPerFloor, MaxChestsPerFloor + 1);
        var chests = new List<Chest>();
        var blocked = new HashSet<Position>();
        var hasMimic = false;

        for (var attempt = 0; attempt < ChestPlacementAttempts && chests.Count < wanted && rooms.Count > 0; attempt++)
        {
            var room = _random.Pick(rooms);
            var candidates = room.Tiles()
                .Where(tile => tile.X > room.X && tile.X < room.X + room.Width - 1)
                .Where(tile => tile.Y > room.Y && tile.Y < room.Y + room.Height - 1)
                .Where(tile => layout.Map.IsWalkable(tile) && !layout.Map.IsLava(tile) && !occupied.Contains(tile))
                .ToList();

            if (candidates.Count == 0)
                continue;

            var tile = _random.Pick(candidates);
            blocked.Add(tile);

            if (!EverythingReachable(layout, blocked))
            {
                blocked.Remove(tile);
                continue;
            }

            occupied.Add(tile);
            Enemy? mimic = null;

            if (!hasMimic && _random.NextDouble() < MimicChance(floor))
            {
                mimic = new Enemy(nextId(), tile, BestiaryCatalog.Get(MimicId), Rarity.Rare, floor);
                hasMimic = true;
            }

            chests.Add(new Chest(tile, mimic));
        }

        return chests;
    }

    private static bool EverythingReachable(FloorLayout layout, HashSet<Position> blocked)
    {
        var map = layout.Map;
        var start = layout.StartRoom.Center;
        var walkable = 0;

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (map.IsWalkable(new Position(x, y)))
                    walkable++;
            }
        }

        var seen = new HashSet<Position> { start };
        var queue = new Queue<Position>([start]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var direction in Enum.GetValues<Direction>())
            {
                var next = current.Move(direction);

                if (map.IsWalkable(next) && !blocked.Contains(next) && seen.Add(next))
                    queue.Enqueue(next);
            }
        }

        return seen.Count == walkable - blocked.Count;
    }

    private List<Biome> RollBiomes(IReadOnlyList<Biome> catalog)
    {
        var biomes = new List<Biome>(RolledFloors);

        while (biomes.Count < RolledFloors)
        {
            var candidates = catalog.Where(biome => biome != biomes.LastOrDefault()).ToList();
            biomes.Add(_random.Pick(candidates));
        }

        return biomes;
    }

    private static Position BossPosition(Room room, HashSet<Position> occupied)
    {
        var center = room.Center;
        var position = occupied.Contains(center) ? center with { X = center.X - 1 } : center;
        occupied.Add(position);
        return position;
    }

    private Position? FreeTile(DungeonMap map, Room room, HashSet<Position> occupied)
    {
        var free = room.Tiles().Where(tile => map.IsWalkable(tile) && !map.IsLava(tile) && !occupied.Contains(tile)).ToList();

        return free.Count == 0 ? null : _random.Pick(free);
    }

    private Rarity RollMonsterRarity(int floor) => Roll(rarity => MonsterRarityChance(rarity, floor));

    private Rarity RollGearRarity(int floor) => Roll(rarity => GearRarityChance(rarity, floor));

    private Rarity Roll(Func<Rarity, double> chance)
    {
        var roll = _random.NextDouble();
        var threshold = 0.0;

        foreach (var rarity in new[] { Rarity.Legendary, Rarity.Epic, Rarity.Rare })
        {
            threshold += chance(rarity);

            if (roll < threshold)
                return rarity;
        }

        return Rarity.Common;
    }

    private ItemDefinition RollGear(int floor)
    {
        var rarity = RollGearRarity(floor);
        var candidates = ItemCatalog.Equipment
            .Where(item => item.Rarity == rarity)
            .ToList();

        return _random.Pick(candidates);
    }
}
