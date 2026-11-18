using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Player : Creature
{
    public const int MaxNameLength = 16;
    public const string DefaultName = "Hero";

    private readonly Dictionary<EquipmentSlot, InventoryEntry> _equipment = [];
    private readonly Dictionary<string, ActiveBoost> _boosts = [];

    public Player(Position position, Stats stats, string? name = null)
        : base(position, stats)
    {
        Facing = Direction.Down;
        Name = NormalizeName(name, DefaultName);
    }

    public Player(Position position, CharacterClass characterClass, string? name = null)
        : this(position, characterClass?.BaseStats ?? throw new ArgumentNullException(nameof(characterClass)), name)
    {
        Class = characterClass;
        Name = NormalizeName(name, characterClass.Name);
    }

    public string Name { get; }

    public CharacterClass? Class { get; }
    public int Level { get; internal set; } = 1;
    public Direction Facing { get; internal set; }

    public Inventory Inventory { get; } = new();

    public IReadOnlyDictionary<EquipmentSlot, InventoryEntry> Equipment => _equipment;

    public IReadOnlyCollection<ActiveBoost> Boosts => _boosts.Values;

    public PotionDefinition? QuickSlot { get; private set; }

    public int StepsPerTurn => Class?.Speed == Speed.Fast ? 2 : 1;

    public ItemInstance? Weapon => EquippedIn(EquipmentSlot.Weapon);

    public Element? Resistance => EquippedIn(EquipmentSlot.Cloak)?.Element;

    public override Stats CombatStats =>
        Stats.Apply(EquippedModifiers().Concat(BoostModifiers()).Concat(MasteryModifiers()));

    public EquipResult Equip(int entryId)
    {
        if (_equipment.Values.Any(equipped => equipped.Id == entryId))
            return EquipResult.AlreadyEquipped;

        var entry = Inventory.Find(entryId);

        if (entry is null)
            return EquipResult.ItemNotFound;

        if (entry.Item.Definition is not EquipmentDefinition equipment)
            return EquipResult.NotEquippable;

        var slot = SlotFor(equipment.Kind);
        Inventory.Take(entryId);

        if (_equipment.TryGetValue(slot, out var replaced))
            Inventory.TryPut(replaced);

        _equipment[slot] = entry;
        return EquipResult.Equipped;
    }

    public UnequipResult Unequip(EquipmentSlot slot)
    {
        if (!_equipment.TryGetValue(slot, out var entry))
            return UnequipResult.SlotEmpty;

        if (!Inventory.TryPut(entry))
            return UnequipResult.BackpackFull;

        _equipment.Remove(slot);
        return UnequipResult.Unequipped;
    }

    public UseResult Use(int entryId)
    {
        var entry = Inventory.Find(entryId);

        if (entry is null)
            return UseResult.ItemNotFound;

        if (entry.Item.Definition is not PotionDefinition potion)
            return UseResult.NotUsable;

        if (potion.Healing > 0 && Health == Stats.MaxHealth)
            return UseResult.AlreadyAtFullHealth;

        if (potion.Healing > 0)
            Heal(potion.Healing);

        if (potion.Boost is { } boost)
            _boosts[potion.Id] = new ActiveBoost(potion, boost, potion.BoostTurns);

        Inventory.RemoveOne(entry.Id);
        return UseResult.Used;
    }

    public UseResult UseQuickSlot()
    {
        if (QuickSlot is null)
            return UseResult.QuickSlotEmpty;

        var entry = Inventory.Entries.FirstOrDefault(candidate => candidate.Item.Id == QuickSlot.Id);

        return entry is null ? UseResult.ItemNotFound : Use(entry.Id);
    }

    public bool AssignQuickSlot(int entryId)
    {
        if (Inventory.Find(entryId)?.Item.Definition is not PotionDefinition potion)
            return false;

        QuickSlot = potion;
        return true;
    }

    internal IReadOnlyList<ActiveBoost> AdvanceBoosts()
    {
        var expired = new List<ActiveBoost>();

        foreach (var boost in _boosts.Values.ToList())
        {
            boost.RemainingTurns--;

            if (boost.RemainingTurns > 0)
                continue;

            _boosts.Remove(boost.Source.Id);
            expired.Add(boost);
        }

        return expired;
    }

    internal void ClearBoosts() => _boosts.Clear();

    private static string NormalizeName(string? name, string fallback)
    {
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return fallback;

        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"A name cannot be longer than {MaxNameLength} characters.", nameof(name));

        return trimmed;
    }

    private ItemInstance? EquippedIn(EquipmentSlot slot) =>
        _equipment.TryGetValue(slot, out var entry) ? entry.Item : null;

    private EquipmentSlot SlotFor(EquipmentKind kind) => kind switch
    {
        EquipmentKind.Weapon => EquipmentSlot.Weapon,
        EquipmentKind.Head => EquipmentSlot.Head,
        EquipmentKind.Body => EquipmentSlot.Body,
        EquipmentKind.Feet => EquipmentSlot.Feet,
        EquipmentKind.Ring => FreeRingSlot(),
        EquipmentKind.Cloak => EquipmentSlot.Cloak,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown equipment kind.")
    };

    private EquipmentSlot FreeRingSlot()
    {
        if (!_equipment.ContainsKey(EquipmentSlot.Ring1))
            return EquipmentSlot.Ring1;

        if (!_equipment.ContainsKey(EquipmentSlot.Ring2))
            return EquipmentSlot.Ring2;

        return EquipmentSlot.Ring1;
    }

    private IEnumerable<StatModifier> EquippedModifiers() =>
        _equipment.Values
            .Select(entry => entry.Item.Definition)
            .OfType<EquipmentDefinition>()
            .SelectMany(item => item.Modifiers);

    private IEnumerable<StatModifier> BoostModifiers() =>
        _boosts.Values.Select(boost => boost.Modifier);

    private IEnumerable<StatModifier> MasteryModifiers()
    {
        if (Class is null || !_equipment.TryGetValue(EquipmentSlot.Weapon, out var entry))
            yield break;

        if (entry.Item.Definition is WeaponDefinition weapon && weapon.WeaponType == Class.Mastery.WeaponType)
            yield return Class.Mastery.Bonus;
    }
}