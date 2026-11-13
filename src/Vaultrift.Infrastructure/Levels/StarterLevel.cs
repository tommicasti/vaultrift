using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
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

    private static readonly Stats WeakEnemy = new(maxHealth: 8, attack: 3, defense: 0, evasion: 5);
    private static readonly Stats StrongEnemy = new(maxHealth: 12, attack: 5, defense: 1, evasion: 0);

    public static Game CreateGame(IRandom random, ClassDefinition playerClass)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        var map = DungeonMap.Parse(Rows);
        var player = new Player(new Position(1, 1), playerClass.Class);
        GiveStartingKit(player, playerClass.StartingItemIds);

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

    private static void GiveStartingKit(Player player, IReadOnlyList<string> itemIds)
    {
        foreach (var itemId in itemIds)
            player.Inventory.TryAdd(ItemCatalog.Get(itemId));

        foreach (var entry in player.Inventory.Entries)
        {
            if (entry.Item is EquipmentDefinition)
                player.Equip(entry.Id);
            else if (entry.Item is PotionDefinition && player.QuickSlot is null)
                player.AssignQuickSlot(entry.Id);
        }
    }

    private static FloorItem Item(int x, int y, string itemId) =>
        new(new Position(x, y), ItemCatalog.Get(itemId));
}