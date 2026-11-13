using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Levels;

public static class StarterLevel
{
    private static readonly string[] Rows =
    [
        "####################",
        "#......#...........#",
        "#......#....####...#",
        "#..........#.......#",
        "#......#...#...#...#",
        "####.###...#...#...#",
        "#..........#...#...#",
        "#..####....#.......#",
        "#..........####....#",
        "####################"
    ];

    private static readonly Stats PlayerStats = new(maxHealth: 30, attack: 2, defense: 2, evasion: 5);
    private static readonly Stats WeakEnemy = new(maxHealth: 8, attack: 3, defense: 0, evasion: 5);
    private static readonly Stats StrongEnemy = new(maxHealth: 12, attack: 5, defense: 1, evasion: 0);

    public static Game CreateGame(IRandom random)
    {
        var map = DungeonMap.Parse(Rows);
        var player = new Player(new Position(1, 1), PlayerStats);
        GiveStartingKit(player);

        Enemy[] enemies =
        [
            new(1, new Position(5, 3), WeakEnemy),
            new(2, new Position(14, 1), WeakEnemy),
            new(3, new Position(2, 6), StrongEnemy),
            new(4, new Position(17, 7), StrongEnemy),
            new(5, new Position(9, 8), WeakEnemy)
        ];

        FloorItem[] items =
        [
            Item(3, 3, "potion-healing"),
            Item(12, 6, "potion-healing"),
            Item(9, 1, "sword-rare"),
            Item(2, 7, "body-common"),
            Item(17, 4, "potion-strength"),
            Item(18, 8, "sword-vaultbreaker")
        ];

        return new Game(map, player, enemies, random, items);
    }

    private static void GiveStartingKit(Player player)
    {
        player.Inventory.TryAdd(ItemCatalog.Get("sword-common"));
        player.Equip(player.Inventory.Entries[0].Id);

        player.Inventory.TryAdd(ItemCatalog.Get("potion-healing"));
        player.Inventory.TryAdd(ItemCatalog.Get("potion-healing"));
        player.AssignQuickSlot(player.Inventory.Entries[1].Id);
    }

    private static FloorItem Item(int x, int y, string itemId) =>
        new(new Position(x, y), ItemCatalog.Get(itemId));
}