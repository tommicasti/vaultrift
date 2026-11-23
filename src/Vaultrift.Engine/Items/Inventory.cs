namespace Vaultrift.Engine.Items;

public sealed class Inventory
{
    public const int DefaultCapacity = 64;
    public const int ExpansionSize = 8;
    public const int MaxCapacity = 96;

    private readonly List<InventoryEntry> _entries = [];
    private int _nextId = 1;

    public Inventory(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

        Capacity = capacity;
    }

    public int Capacity { get; private set; }

    public bool CanExpand => Capacity + ExpansionSize <= MaxCapacity;

    public int Expansions => Math.Max(0, (Capacity - DefaultCapacity) / ExpansionSize);

    internal bool Expand()
    {
        if (!CanExpand)
            return false;

        Capacity += ExpansionSize;
        return true;
    }

    public IReadOnlyList<InventoryEntry> Entries => _entries;

    public int NextEntryId => _nextId;

    internal void Restore(int capacity, int nextEntryId, IEnumerable<InventoryEntry> entries)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentNullException.ThrowIfNull(entries);

        Capacity = capacity;
        _nextId = nextEntryId;
        _entries.Clear();
        _entries.AddRange(entries);
    }

    public bool IsFull => _entries.Count >= Capacity;

    public bool TryAdd(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsStackable(item))
        {
            var stack = _entries.FirstOrDefault(entry => entry.Item == item);

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

    internal InventoryEntry CreateEntry(ItemInstance item) => new(_nextId++, item);

    public bool TryAddBound(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsFull)
            return false;

        _entries.Add(new InventoryEntry(_nextId++, item) { Mark = ItemMark.Favorite });
        return true;
    }

    public bool CanAdd(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return !IsFull || (IsStackable(item) && _entries.Any(entry => entry.Item == item));
    }

    public InventoryEntry? Find(int entryId) =>
        _entries.FirstOrDefault(entry => entry.Id == entryId);

    internal InventoryEntry? Take(int entryId)
    {
        var entry = Find(entryId);

        if (entry is not null)
            _entries.Remove(entry);

        return entry;
    }

    internal bool TryPut(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (IsFull)
            return false;

        _entries.Add(entry);
        return true;
    }

    public bool Remove(int entryId)
    {
        var entry = Find(entryId);

        return entry is not null && _entries.Remove(entry);
    }

    public IReadOnlyList<InventoryEntry> RemoveJunk()
    {
        var junk = _entries.Where(entry => entry.Mark == ItemMark.Junk).ToList();
        _entries.RemoveAll(entry => entry.Mark == ItemMark.Junk);
        return junk;
    }

    public void RemoveOne(int entryId)
    {
        var entry = Find(entryId)
            ?? throw new KeyNotFoundException($"No inventory entry with id {entryId}.");

        entry.Quantity--;

        if (entry.Quantity == 0)
            _entries.Remove(entry);
    }

    private static bool IsStackable(ItemInstance item) => item.Definition is PotionDefinition or ScrollDefinition or RuneDefinition;
}