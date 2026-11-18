using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Levels;

public static class VaultDungeon
{
    public static Game CreateGame(IRandom random, ClassDefinition playerClass, string? playerName = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(playerClass);

        var player = new Player(new Position(0, 0), playerClass.Class, playerName);
        GiveStartingKit(player, playerClass.StartingItemIds);

        return new Game(player, new VaultFloors(random), random, new VaultLoot(random));
    }

    private static void GiveStartingKit(Player player, IReadOnlyList<string> itemIds)
    {
        foreach (var itemId in itemIds)
            player.Inventory.TryAdd(new ItemInstance(ItemCatalog.Get(itemId)));

        foreach (var entry in player.Inventory.Entries.ToList())
        {
            if (entry.Item.Definition is EquipmentDefinition)
                player.Equip(entry.Id);
            else if (entry.Item.Definition is PotionDefinition && player.QuickSlot is null)
                player.AssignQuickSlot(entry.Id);
        }
    }
}