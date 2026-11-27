using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed record LiveGrave(int Id, Guid Owner, string OwnerName, Position Tile, IReadOnlyList<(EquipmentSlot Slot, InventoryEntry Entry)> Items)
{
    public int Count => Items.Count;
}

public sealed record LiveTotem(Position Tile, Enemy Boss, int Price);
