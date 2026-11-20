using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Levels;

public static class RiftArena
{
    public const string BiomeId = "rift";
    public const string GuardianId = "custode";

    private static readonly string[] Layout =
    [
        "#########################",
        "########.........########",
        "######.............######",
        "####.................####",
        "###.....o.......o.....###",
        "##.....................##",
        "##.....................##",
        "#....o.............o....#",
        "#.......................#",
        "#.......................#",
        "##.....................##",
        "##.....o.........o.....##",
        "###...................###",
        "####......o...o......####",
        "######.............######",
        "########.........########",
        "#########################",
    ];

    private static readonly Position GuardianStart = new(12, 4);
    private static readonly Position HeroStart = new(12, 14);
    public static readonly Position Waystone = new(11, 14);
    private static readonly Position[] Potions = [new(11, 13), new(13, 13)];

    public static Floor Create(int firstId, int levelBonus = 0)
    {
        var map = DungeonMap.Parse(Layout);
        var guardian = new Enemy(firstId, GuardianStart, BestiaryCatalog.Get(GuardianId), Rarity.Epic, VaultFloors.FinalFloor + levelBonus);
        var items = Potions
            .Select(position => new FloorItem(position, new ItemInstance(ItemCatalog.Get("potion-greater-healing"))))
            .ToList();

        return new Floor(map, HeroStart, [guardian], items, Stairs: null, BiomeId, Chests: [], Merchant: null, Waystone);
    }

    public static IReadOnlyList<string> Rows() => Layout;
}
