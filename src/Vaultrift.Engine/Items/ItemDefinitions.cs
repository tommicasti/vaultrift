namespace Vaultrift.Engine.Items;

public abstract record ItemDefinition(string Id, string Name, Rarity Rarity, string? Description);

public abstract record EquipmentDefinition(
    string Id,
    string Name,
    Rarity Rarity,
    EquipmentKind Kind,
    IReadOnlyList<StatModifier> Modifiers,
    string? Description,
    Element? FixedElement)
    : ItemDefinition(Id, Name, Rarity, Description)
{
    public IReadOnlyList<ItemEffect> Effects { get; init; } = [];
}

public sealed record WeaponDefinition(
    string Id,
    string Name,
    Rarity Rarity,
    WeaponType WeaponType,
    IReadOnlyList<StatModifier> Modifiers,
    string? Description = null,
    Element? FixedElement = null)
    : EquipmentDefinition(Id, Name, Rarity, EquipmentKind.Weapon, Modifiers, Description, FixedElement)
{
    public bool IsTwoHanded => WeaponType.IsTwoHanded();
}

public sealed record GearDefinition(
    string Id,
    string Name,
    Rarity Rarity,
    EquipmentKind Kind,
    IReadOnlyList<StatModifier> Modifiers,
    string? Description = null,
    Element? FixedElement = null)
    : EquipmentDefinition(Id, Name, Rarity, Kind, Modifiers, Description, FixedElement)
{
    public string? ClassId { get; init; }
}

public sealed record PotionDefinition(
    string Id,
    string Name,
    Rarity Rarity,
    int Healing = 0,
    StatModifier? Boost = null,
    int BoostTurns = 0,
    string? Description = null,
    int ManaRestore = 0)
    : ItemDefinition(Id, Name, Rarity, Description);
