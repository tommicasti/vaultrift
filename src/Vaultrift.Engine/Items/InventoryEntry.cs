namespace Vaultrift.Engine.Items;

public sealed class InventoryEntry
{
    internal InventoryEntry(int id, ItemInstance item)
    {
        Id = id;
        Item = item;
        Quantity = 1;
    }

    public int Id { get; }

    public ItemInstance Item { get; }

    public int Quantity { get; internal set; }
}