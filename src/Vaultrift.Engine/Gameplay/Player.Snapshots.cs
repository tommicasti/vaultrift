using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Persistence;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Player
{
    public PlayerSnapshot Capture() => new(
        Class?.Id,
        Name,
        new StatsSnapshot(_baseStats.MaxHealth, _baseStats.Attack, _baseStats.Defense, _baseStats.Evasion,
            _baseStats.Critical, _baseStats.Block, _baseStats.MaxMana),
        Level,
        Experience,
        UnspentPoints,
        new Dictionary<StatPoint, int>(_spentPoints),
        Health,
        Mana,
        Coins,
        Position,
        Facing,
        AbilityCooldown,
        BulwarkTurns,
        FuryTurns,
        InvisibleTurns,
        PoisonTurns,
        IsOverloaded,
        ActiveSpellIndex,
        new InventorySnapshot(Inventory.Capacity, Inventory.NextEntryId, Inventory.Entries.Select(CaptureEntry).ToList()),
        _equipment.ToDictionary(pair => pair.Key, pair => CaptureEntry(pair.Value)),
        _quickSlots.Select(item => item?.Id).ToList(),
        _spellSlots.Select(scroll => scroll?.Id).ToList(),
        _boosts.Values.Select(boost => new BoostSnapshot(boost.Source.Id, boost.RemainingTurns)).ToList());

    public StatsPreview PreviewEquip(int entryId, EquipmentSlot? hand, IGameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var copy = Restore(Capture(), content);
        var result = copy.Equip(entryId, hand);

        return StatsPreview.Of(result, copy);
    }

    public static Player Restore(PlayerSnapshot snapshot, IGameContent content)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(content);

        var baseStats = snapshot.BaseStats;
        var player = snapshot.ClassId is { } classId
            ? new Player(snapshot.Position, content.Class(classId), snapshot.Name)
            : new Player(snapshot.Position, new Stats(baseStats.MaxHealth, baseStats.Attack, baseStats.Defense,
                baseStats.Evasion, baseStats.Critical, baseStats.Block, baseStats.MaxMana), snapshot.Name);

        player.Level = snapshot.Level;
        player.Experience = snapshot.Experience;
        player.UnspentPoints = snapshot.UnspentPoints;

        foreach (var (stat, points) in snapshot.SpentPoints)
            player._spentPoints[stat] = points;

        player.Stats = Progression.Apply(player._baseStats, player._spentPoints);
        player.Inventory.Restore(snapshot.Inventory.Capacity, snapshot.Inventory.NextEntryId,
            snapshot.Inventory.Entries.Select(entry => RestoreEntry(entry, content)));

        foreach (var (slot, entry) in snapshot.Equipment)
            player._equipment[slot] = RestoreEntry(entry, content);

        for (var slot = 0; slot < Math.Min(QuickSlotCount, snapshot.QuickSlots.Count); slot++)
            player._quickSlots[slot] = snapshot.QuickSlots[slot] is { } itemId ? content.Item(itemId) : null;

        for (var slot = 0; slot < Math.Min(MaxLearnedSpells, snapshot.SpellSlots.Count); slot++)
            player._spellSlots[slot] = snapshot.SpellSlots[slot] is { } scrollId ? (ScrollDefinition)content.Item(scrollId) : null;

        foreach (var boost in snapshot.Boosts)
        {
            var potion = (PotionDefinition)content.Item(boost.PotionId);
            player._boosts[potion.Id] = new ActiveBoost(potion, potion.Boost ?? throw new InvalidOperationException($"{potion.Id} gives no boost."), boost.RemainingTurns);
        }

        player.Facing = snapshot.Facing;
        player.Mana = snapshot.Mana;
        player.Coins = snapshot.Coins;
        player.AbilityCooldown = snapshot.AbilityCooldown;
        player.BulwarkTurns = snapshot.BulwarkTurns;
        player.FuryTurns = snapshot.FuryTurns;
        player.InvisibleTurns = snapshot.InvisibleTurns;
        player.PoisonTurns = snapshot.PoisonTurns;
        player.IsOverloaded = snapshot.IsOverloaded;
        player.ActiveSpellIndex = snapshot.ActiveSpellIndex;
        player.RestoreHealth(snapshot.Health);
        return player;
    }

    internal static EntrySnapshot CaptureEntry(InventoryEntry entry) =>
        new(entry.Id, CaptureItem(entry.Item), entry.Quantity, entry.Mark);

    internal static ItemSnapshot CaptureItem(ItemInstance item) => new(item.Id, item.Element);

    internal static ItemInstance RestoreItem(ItemSnapshot item, IGameContent content) =>
        new(content.Item(item.Id), item.Element);

    private static InventoryEntry RestoreEntry(EntrySnapshot entry, IGameContent content) =>
        new(entry.Id, RestoreItem(entry.Item, content)) { Quantity = entry.Quantity, Mark = entry.Mark };
}
