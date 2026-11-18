using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Tests.Support;

public static class Equipping
{
    public static EquipResult Give(this Player player, ItemInstance item, EquipmentSlot? hand = null)
    {
        player.Inventory.TryAdd(item);
        return player.Equip(player.Inventory.Entries[^1].Id, hand);
    }
}
