using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Persistence;

public static class GraveItems
{
    public static IReadOnlyList<GraveItemSnapshot> Capture(IEnumerable<(EquipmentSlot Slot, InventoryEntry Entry)> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Select(item => new GraveItemSnapshot(item.Slot, Player.CaptureEntry(item.Entry))).ToList();
    }

    public static IReadOnlyList<(EquipmentSlot Slot, InventoryEntry Entry)> Restore(IEnumerable<GraveItemSnapshot> items, IGameContent content)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(content);
        return items.Select(item => (item.Slot, Player.RestoreEntry(item.Entry, content))).ToList();
    }
}
