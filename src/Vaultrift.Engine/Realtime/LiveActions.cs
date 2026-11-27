using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public static class LiveActions
{
    public static TurnResult Equip(Player player, int entryId, EquipmentSlot? hand = null)
    {
        ArgumentNullException.ThrowIfNull(player);

        var item = player.Inventory.Find(entryId)?.Item;
        var result = player.Equip(entryId, hand);

        GameEvent outcome = result == EquipResult.Equipped ? new ItemEquipped(item!) : new EquipRejected(result);
        return new TurnResult(TurnPassed: false, [outcome]);
    }

    public static TurnResult Unequip(Player player, EquipmentSlot slot)
    {
        ArgumentNullException.ThrowIfNull(player);

        var item = player.Equipment.TryGetValue(slot, out var entry) ? entry.Item : null;
        var result = player.Unequip(slot);

        GameEvent outcome = result == UnequipResult.Unequipped ? new ItemUnequipped(item!) : new UnequipRejected(result);
        return new TurnResult(TurnPassed: false, [outcome]);
    }

    public static TurnResult Use(Player player, int entryId)
    {
        ArgumentNullException.ThrowIfNull(player);

        var entry = player.Inventory.Find(entryId);

        if (entry?.Item.Definition is ScrollDefinition)
            return Rejected(UseResult.NotUsable);

        return Resolve(player, entry?.Item, player.Use(entryId));
    }

    public static TurnResult UseQuickSlot(Player player, int slot)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.QuickSlotEntry(slot) is { Item.Definition: ScrollDefinition })
            return Rejected(UseResult.NotUsable);

        var item = player.QuickSlots.ElementAtOrDefault(slot) is { } definition ? new ItemInstance(definition) : null;
        return Resolve(player, item, player.UseQuickSlot(slot));
    }

    private static TurnResult Resolve(Player player, ItemInstance? item, UseResult result)
    {
        if (result != UseResult.Used)
            return Rejected(result);

        var events = new List<GameEvent> { new ItemUsed(item!) };

        if (item!.Definition is RuneDefinition && player.Weapon is { } imbued)
            events.Add(new WeaponImbued(imbued));

        return new TurnResult(TurnPassed: false, events);
    }

    private static TurnResult Rejected(UseResult result) => new(TurnPassed: false, [new UseRejected(result)]);
}
