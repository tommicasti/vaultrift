namespace Vaultrift.Engine.Items;

public abstract record ItemDefinition(string Id, string Name, Rarity Rarity);

public abstract record EquipmentDefinition(string Id, string Name, Rarity Rarity, IReadOnlyList<StatModifier> Modifiers)
    : ItemDefinition(Id, Name, Rarity);

public sealed record WeaponDefinition(string Id, string Name, Rarity Rarity, WeaponType WeaponType, IReadOnlyList<StatModifier> Modifiers)
    : EquipmentDefinition(Id, Name, Rarity, Modifiers);

public sealed record ArmorDefinition(string Id, string Name, Rarity Rarity, IReadOnlyList<StatModifier> Modifiers)
    : EquipmentDefinition(Id, Name, Rarity, Modifiers);

public sealed record PotionDefinition(string Id, string Name, Rarity Rarity, int Healing = 0, StatModifier? Boost = null, int BoostTurns = 0)
    : ItemDefinition(Id, Name, Rarity);