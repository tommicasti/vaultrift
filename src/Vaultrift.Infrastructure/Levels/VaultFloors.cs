using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Levels;

public sealed class VaultFloors : IFloorProvider
{
    public static IReadOnlyList<FloorTheme> Themes { get; } =
    [
        new("rat-king", ["rat", "bat", "slime"]),
        new("spider-queen", ["rat", "bat", "spider", "slime", "ghost"]),
        new("troll", ["bat", "spider", "slime", "ghost", "cyclops"])
    ];

    private const int IdsPerFloor = 1000;
    private const int GearPerFloor = 4;
    private const int PotionsPerFloor = 3;

    private static readonly string[] Potions =
        ["potion-healing", "potion-healing", "potion-greater-healing", "potion-strength", "potion-protection"];

    private readonly IRandom _random;

    public VaultFloors(IRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public int Count => Themes.Count;

    public Floor Create(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, Count);

        var theme = Themes[number - 1];
        var layout = FloorGenerator.Generate(_random);
        var start = layout.StartRoom.Center;
        var stairs = number < Count ? layout.BossRoom.Center : (Position?)null;
        var occupied = new HashSet<Position> { start };
        var nextId = number * IdsPerFloor;

        if (stairs is { } stairsPosition)
            occupied.Add(stairsPosition);

        var enemies = new List<Enemy>
        {
            new(nextId++, BossPosition(layout.BossRoom, occupied), BestiaryCatalog.Get(theme.BossId))
        };

        foreach (var room in layout.Rooms.Where(room => room != layout.StartRoom && room != layout.BossRoom))
        {
            var count = _random.Next(1, 3 + number / 2);

            for (var index = 0; index < count && FreeTile(room, occupied) is { } position; index++)
            {
                occupied.Add(position);
                var kind = BestiaryCatalog.Get(_random.Pick(theme.MonsterIds));
                enemies.Add(new Enemy(nextId++, position, kind, RollRarity(number), number));
            }
        }

        var lootRooms = layout.Rooms.Where(room => room != layout.BossRoom).ToList();
        var items = new List<FloorItem>();

        for (var index = 0; index < GearPerFloor + PotionsPerFloor; index++)
        {
            var definition = index < GearPerFloor ? RollGear(number) : ItemCatalog.Get(_random.Pick(Potions));

            if (FreeTile(_random.Pick(lootRooms), occupied) is not { } position)
                continue;

            occupied.Add(position);
            items.Add(new FloorItem(position, ItemRoller.Roll(definition, _random)));
        }

        return new Floor(layout.Map, start, enemies, items, stairs);
    }

    private static Position BossPosition(Room room, HashSet<Position> occupied)
    {
        var center = room.Center;
        var position = occupied.Contains(center) ? center with { X = center.X - 1 } : center;
        occupied.Add(position);
        return position;
    }

    private Position? FreeTile(Room room, HashSet<Position> occupied)
    {
        var free = room.Tiles().Where(tile => !occupied.Contains(tile)).ToList();

        return free.Count == 0 ? null : _random.Pick(free);
    }

    private Rarity RollRarity(int floor)
    {
        var roll = _random.NextDouble();
        var bonus = (floor - 1) * 0.05;

        if (roll < 0.02 + bonus / 2) return Rarity.Legendary;
        if (roll < 0.10 + bonus) return Rarity.Epic;
        if (roll < 0.30 + bonus * 2) return Rarity.Rare;
        return Rarity.Common;
    }

    private ItemDefinition RollGear(int floor)
    {
        var rarity = RollRarity(floor);
        var candidates = ItemCatalog.All
            .OfType<EquipmentDefinition>()
            .Where(item => item.Rarity == rarity)
            .ToList();

        return _random.Pick(candidates);
    }
}