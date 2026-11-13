using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Player : Creature
{
    private readonly Dictionary<EquipmentSlot, InventoryEntry> _equipment = [];
    private readonly Dictionary<string, ActiveBoost> _boosts = [];

    public Player(Position position, Stats stats)
        : base(position, stats)
    {
        Facing = Direction.Down;
    }

    public Player(Position position, CharacterClass characterClass)
        : this(position, characterClass?.BaseStats ?? throw new ArgumentNullException(nameof(characterClass)))
    {
        Class = characterClass;
    }

    public CharacterClass? Class { get; }

    public Direction Facing { get; internal set; }

    public Inventory Inventory { get; } = new();

    public IReadOnlyDictionary<EquipmentSlot, InventoryEntry> Equipment => _equipment;

    public IReadOnlyCollection<ActiveBoost> Boosts => _boosts.Values;

    public PotionDefinition? QuickSlot { get; private set; }

    public override Stats CombatStats =>
        Stats.Apply(EquippedModifiers().Concat(BoostModifiers()).Concat(MasteryModifiers()));

    public EquipResult Equip(int entryId)
    {
        var entry = Inventory.Find(entryId);

        if (entry is null)
            return EquipResult.ItemNotFound;

        if (entry.Item is not EquipmentDefinition equipment)
            return EquipResult.NotEquippable;

        if (_equipment.ContainsValue(entry))
            return EquipResult.AlreadyEquipped;

        _equipment[SlotFor(equipment.Kind)] = entry;
        return EquipResult.Equipped;
    }

    public UseResult Use(int entryId)
    {
        var entry = Inventory.Find(entryId);

        if (entry is null)
            return UseResult.ItemNotFound;

        if (entry.Item is not PotionDefinition potion)
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
        if (Inventory.Find(entryId)?.Item is not PotionDefinition potion)
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

    private EquipmentSlot SlotFor(EquipmentKind kind) => kind switch
    {
        EquipmentKind.Weapon => EquipmentSlot.Weapon,
        EquipmentKind.Head => EquipmentSlot.Head,
        EquipmentKind.Body => EquipmentSlot.Body,
        EquipmentKind.Feet => EquipmentSlot.Feet,
        EquipmentKind.Ring => FreeRingSlot(),
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
            .Select(entry => entry.Item)
            .OfType<EquipmentDefinition>()
            .SelectMany(item => item.Modifiers);

    private IEnumerable<StatModifier> BoostModifiers() =>
        _boosts.Values.Select(boost => boost.Modifier);

    private IEnumerable<StatModifier> MasteryModifiers()
    {
        if (Class is null || !_equipment.TryGetValue(EquipmentSlot.Weapon, out var entry))
            yield break;

        if (entry.Item is WeaponDefinition weapon && weapon.WeaponType == Class.Mastery.WeaponType)
            yield return Class.Mastery.Bonus;
    }
}