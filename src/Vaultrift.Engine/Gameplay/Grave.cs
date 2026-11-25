using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed record Grave(Position Position, IReadOnlyList<(EquipmentSlot Slot, InventoryEntry Entry)> Items, int Floor = 1)
{
    public int Count => Items.Count;
}
