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

    public bool IsBound { get; init; }

    public bool ScalesWithLevel { get; init; }
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
    public bool IsTwoHanded => WeaponType.IsTwoHanded() || FillsBothHands;

    public SpellDefinition? Spell { get; init; }

    public bool IsSpare { get; init; }

    public ExoticPower? Exotic { get; init; }

    public bool IsTwin { get; init; }

    public bool FillsBothHands { get; init; }

    public bool Pierces { get; init; }
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

public sealed record ScrollDefinition(
    string Id,
    string Name,
    Rarity Rarity,
    SpellDefinition Spell,
    string? Description = null)
    : ItemDefinition(Id, Name, Rarity, Description);

public sealed record RuneDefinition(
    string Id,
    string Name,
    Rarity Rarity,
    Element Element,
    string? Description = null)
    : ItemDefinition(Id, Name, Rarity, Description);
