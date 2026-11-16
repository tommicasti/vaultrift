namespace Vaultrift.Engine.Items;

public sealed class Inventory
{
    public const int DefaultCapacity = 40;

    private readonly List<InventoryEntry> _entries = [];
    private int _nextId = 1;

    public Inventory(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

        Capacity = capacity;
    }

    public int Capacity { get; }

    public IReadOnlyList<InventoryEntry> Entries => _entries;

    public bool IsFull => _entries.Count >= Capacity;

    public bool TryAdd(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsStackable(item))
        {
            var stack = _entries.FirstOrDefault(entry => entry.Item.Id == item.Id);

            if (stack is not null)
            {
                stack.Quantity++;
                return true;
            }
        }

        if (IsFull)
            return false;

        _entries.Add(new InventoryEntry(_nextId++, item));
        return true;
    }

    public InventoryEntry? Find(int entryId) =>
        _entries.FirstOrDefault(entry => entry.Id == entryId);

    public void RemoveOne(int entryId)
    {
        var entry = Find(entryId)
            ?? throw new KeyNotFoundException($"No inventory entry with id {entryId}.");

        entry.Quantity--;

        if (entry.Quantity == 0)
            _entries.Remove(entry);
    }

    private static bool IsStackable(ItemDefinition item) => item is PotionDefinition;
}