using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Content;

public static class ItemCatalog
{
    public static IReadOnlyList<ItemDefinition> All { get; } =
    [
        Weapon("sword-common", "Rusty Sword", WeaponType.Sword, Rarity.Common, Attack(3)),
        Weapon("sword-rare", "Steel Sword", WeaponType.Sword, Rarity.Rare, Attack(5)),
        Weapon("sword-epic", "Runed Blade", WeaponType.Sword, Rarity.Epic, Attack(8), Defense(1)),
        Weapon("sword-legendary", "Vaultbreaker", WeaponType.Sword, Rarity.Legendary, Attack(12), Defense(2), Evasion(5)),

        Weapon("axe-common", "Hatchet", WeaponType.Axe, Rarity.Common, Attack(2)),
        Weapon("axe-rare", "Battle Axe", WeaponType.Axe, Rarity.Rare, Attack(3)),
        Weapon("axe-epic", "Bonecleaver", WeaponType.Axe, Rarity.Epic, Attack(5), Defense(2)),
        Weapon("axe-legendary", "Riftsplitter", WeaponType.Axe, Rarity.Legendary, Attack(8), Defense(3), Evasion(3)),

        Weapon("bow-common", "Short Bow", WeaponType.Bow, Rarity.Common, Attack(2)),
        Weapon("bow-rare", "Hunter's Bow", WeaponType.Bow, Rarity.Rare, Attack(3)),
        Weapon("bow-epic", "Whisperwind", WeaponType.Bow, Rarity.Epic, Attack(5), Evasion(5)),
        Weapon("bow-legendary", "Starshot", WeaponType.Bow, Rarity.Legendary, Attack(8), Evasion(8), Defense(1)),

        Armor("armor-common", "Leather Vest", Rarity.Common, Defense(1)),
        Armor("armor-rare", "Chainmail", Rarity.Rare, Defense(2)),
        Armor("armor-epic", "Warden's Plate", Rarity.Epic, Defense(4), Attack(1)),
        Armor("armor-legendary", "Aegis of the Rift", Rarity.Legendary, Defense(6), Attack(2), Evasion(10)),

        new PotionDefinition("potion-healing", "Healing Potion", Rarity.Common, Healing: 10),
        new PotionDefinition("potion-greater-healing", "Greater Healing Potion", Rarity.Rare, Healing: 25),
        new PotionDefinition("potion-strength", "Strength Potion", Rarity.Rare, Boost: Attack(3), BoostTurns: 10),
        new PotionDefinition("potion-protection", "Protection Potion", Rarity.Rare, Boost: Defense(3), BoostTurns: 10)
    ];

    public static ItemDefinition Get(string id) =>
        All.FirstOrDefault(item => item.Id == id)
        ?? throw new KeyNotFoundException($"No item with id '{id}' exists in the catalog.");

    private static StatModifier Attack(int amount) => new(StatType.Attack, amount);

    private static StatModifier Defense(int amount) => new(StatType.Defense, amount);

    private static StatModifier Evasion(int amount) => new(StatType.Evasion, amount);

    private static WeaponDefinition Weapon(string id, string name, WeaponType type, Rarity rarity, params StatModifier[] modifiers) =>
        new(id, name, rarity, type, modifiers);

    private static ArmorDefinition Armor(string id, string name, Rarity rarity, params StatModifier[] modifiers) =>
        new(id, name, rarity, modifiers);
}