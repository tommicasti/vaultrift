using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Player : Creature
{
    private readonly Dictionary<EquipmentSlot, InventoryEntry> _equipment = [];

    public Player(Position position, Stats stats)
        : base(position, stats)
    {
        Facing = Direction.Down;
    }

    public Direction Facing { get; internal set; }

    public Inventory Inventory { get; } = new();

    public IReadOnlyDictionary<EquipmentSlot, InventoryEntry> Equipment => _equipment;

    public override Stats CombatStats => Stats.Apply(EquippedModifiers());

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
}