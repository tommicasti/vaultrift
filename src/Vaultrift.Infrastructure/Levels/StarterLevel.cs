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

    public static Game CreateGame(IRandom random, ClassDefinition playerClass, string? playerName = null)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        var map = DungeonMap.Parse(Rows);
        var player = new Player(new Position(1, 1), playerClass.Class, playerName);
        GiveStartingKit(player, playerClass.StartingItemIds);

        Enemy[] enemies =
        [
            Monster(1, 5, 3, "rat", Rarity.Common, level: 2),
            Monster(2, 14, 1, "bat", Rarity.Rare),
            Monster(3, 2, 6, "spider", Rarity.Epic),
            Monster(4, 17, 7, "cyclops", Rarity.Rare),
            Monster(5, 9, 8, "slime", Rarity.Common),
            Monster(6, 13, 5, "ghost", Rarity.Legendary)
        ];

        FloorItem[] items =
        [
            Item(3, 3, "potion-healing", random),
            Item(12, 6, "potion-healing", random),
            Item(9, 1, "sword-rare", random),
            Item(2, 7, "body-common", random),
            Item(6, 1, "cloak-common", random),
            Item(17, 4, "potion-strength", random),
            Item(14, 3, "sword-ember-oath", random),
            Item(18, 8, "sword-vaultbreaker", random)
        ];

        return new Game(map, player, enemies, random, items);
    }

    private static void GiveStartingKit(Player player, IReadOnlyList<string> itemIds)
    {
        foreach (var itemId in itemIds)
            player.Inventory.TryAdd(new ItemInstance(ItemCatalog.Get(itemId)));

        foreach (var entry in player.Inventory.Entries)
        {
            if (entry.Item.Definition is EquipmentDefinition)
                player.Equip(entry.Id);
            else if (entry.Item.Definition is PotionDefinition && player.QuickSlot is null)
                player.AssignQuickSlot(entry.Id);
        }
    }

    private static Enemy Monster(int id, int x, int y, string kindId, Rarity rarity, int level = 1) =>
        new(id, new Position(x, y), BestiaryCatalog.Get(kindId), rarity, level);

    private static FloorItem Item(int x, int y, string itemId, IRandom random) =>
        new(new Position(x, y), ItemRoller.Roll(ItemCatalog.Get(itemId), random));
}