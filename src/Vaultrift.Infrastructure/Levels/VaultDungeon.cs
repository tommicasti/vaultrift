using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Persistence;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Levels;

public static class VaultDungeon
{
    public static Game CreateGame(IRandom random, ClassDefinition playerClass, string? playerName = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(playerClass);

        return CreateGame(random, new VaultFloors(random, playerClass.Class.Id), playerClass, playerName);
    }

    public static Game CreateGame(IRandom random, VaultFloors floors, ClassDefinition playerClass, string? playerName = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(floors);
        ArgumentNullException.ThrowIfNull(playerClass);

        return new Game(CreateHero(playerClass, playerName), floors, random, new VaultLoot(random)) { Difficulty = floors.Difficulty };
    }

    public static Player CreateHero(ClassDefinition playerClass, string? playerName = null)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        var player = new Player(new Position(0, 0), playerClass.Class, playerName);
        GiveStartingKit(player, playerClass.StartingItemIds);
        return player;
    }

    public static Game RestoreGame(GameSnapshot snapshot, IRandom random, VaultFloors floors)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(floors);

        return Game.Restore(snapshot, VaultContent.Instance, floors, random, new VaultLoot(random));
    }

    private static void GiveStartingKit(Player player, IReadOnlyList<string> itemIds)
    {
        foreach (var itemId in itemIds)
            player.Inventory.TryAdd(new ItemInstance(ItemCatalog.Get(itemId)));

        foreach (var entry in player.Inventory.Entries.ToList())
        {
            if (entry.Item.Definition is EquipmentDefinition)
                player.Equip(entry.Id);
            else if (entry.Item.Definition is PotionDefinition && FreeQuickSlot(player) is { } free)
                player.AssignQuickSlot(entry.Id, free);
        }
    }

    private static int? FreeQuickSlot(Player player)
    {
        for (var slot = 0; slot < player.QuickSlots.Count; slot++)
        {
            if (player.QuickSlots[slot] is null)
                return slot;
        }

        return null;
    }
}
