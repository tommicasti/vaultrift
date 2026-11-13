using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Content;

public static class ItemCatalog
{
    public static IReadOnlyList<ItemDefinition> All { get; } =
    [
        Weapon("sword-common", "Rusty Sword", WeaponType.Sword, Rarity.Common, Attack(3)),
        Weapon("sword-rare", "Steel Sword", WeaponType.Sword, Rarity.Rare, Attack(5)),
        Weapon("sword-epic", "Runed Blade", WeaponType.Sword, Rarity.Epic, Attack(8), Defense(1)),
        LegendaryWeapon("sword-vaultbreaker", "Vaultbreaker", WeaponType.Sword,
            "Every vault has a weakness. This blade found most of them.",
            Attack(12), Defense(2), Evasion(5)),
        LegendaryWeapon("sword-twinfang", "Twinfang", WeaponType.Sword,
            "One strike for the wound, one for the lesson.",
            Attack(11), Evasion(6), Defense(1)),
        LegendaryWeapon("sword-ember-oath", "Ember Oath", WeaponType.Sword,
            "Sworn in fire, kept in ash.",
            Attack(13), Defense(1), Evasion(2)),

        Weapon("axe-common", "Hatchet", WeaponType.Axe, Rarity.Common, Attack(2)),
        Weapon("axe-rare", "Battle Axe", WeaponType.Axe, Rarity.Rare, Attack(3)),
        Weapon("axe-epic", "Bonecleaver", WeaponType.Axe, Rarity.Epic, Attack(5), Defense(2)),
        LegendaryWeapon("axe-riftsplitter", "Riftsplitter", WeaponType.Axe,
            "The rift was here first. The axe just widened it.",
            Attack(8), Defense(3), Evasion(3)),
        LegendaryWeapon("axe-grudgebearer", "Grudgebearer", WeaponType.Axe,
            "It remembers every blow. So should you.",
            Attack(7), Defense(4), Evasion(2)),
        LegendaryWeapon("axe-stormmaw", "Stormmaw", WeaponType.Axe,
            "Thunder doesn't ask permission.",
            Attack(9), Defense(2), Evasion(2)),

        Weapon("bow-common", "Short Bow", WeaponType.Bow, Rarity.Common, Attack(2)),
        Weapon("bow-rare", "Hunter's Bow", WeaponType.Bow, Rarity.Rare, Attack(3)),
        Weapon("bow-epic", "Whisperwind", WeaponType.Bow, Rarity.Epic, Attack(5), Evasion(5)),
        LegendaryWeapon("bow-starshot", "Starshot", WeaponType.Bow,
            "Aim for one. Hit the one behind him too.",
            Attack(8), Evasion(8), Defense(1)),
        LegendaryWeapon("bow-nightglass", "Nightglass", WeaponType.Bow,
            "Sleep is just a slower kind of surrender.",
            Attack(9), Evasion(6), Defense(1)),
        LegendaryWeapon("bow-galecaller", "Galecaller", WeaponType.Bow,
            "Ask the wind nicely. Then pull.",
            Attack(7), Evasion(10), Defense(1)),

        Gear("head-common", "Leather Cap", EquipmentKind.Head, Rarity.Common, Defense(1)),
        Gear("head-rare", "Iron Helm", EquipmentKind.Head, Rarity.Rare, Defense(2)),
        Gear("head-epic", "Warden's Visage", EquipmentKind.Head, Rarity.Epic, Defense(3), Evasion(3)),
        LegendaryGear("head-crown-of-echoes", "Crown of Echoes", EquipmentKind.Head,
            "Dodge once. Strike twice as true.",
            Defense(4), Evasion(5), Attack(1)),
        LegendaryGear("head-mask-of-the-hollow", "Mask of the Hollow", EquipmentKind.Head,
            "Wear no face, wake no one.",
            Defense(4), Evasion(4), Attack(2)),
        LegendaryGear("head-seers-visor", "Seer's Visor", EquipmentKind.Head,
            "The dark has edges. Now you can see them.",
            Defense(5), Evasion(3), Attack(1)),

        Gear("body-common", "Leather Vest", EquipmentKind.Body, Rarity.Common, Defense(1)),
        Gear("body-rare", "Chainmail", EquipmentKind.Body, Rarity.Rare, Defense(2)),
        Gear("body-epic", "Warden's Plate", EquipmentKind.Body, Rarity.Epic, Defense(4), Attack(1)),
        LegendaryGear("body-aegis-of-the-rift", "Aegis of the Rift", EquipmentKind.Body,
            "The first blow is a question. The Aegis answers no.",
            Defense(6), Attack(2), Evasion(10)),
        LegendaryGear("body-bramblehide", "Bramblehide", EquipmentKind.Body,
            "Touch it and it touches back.",
            Defense(6), Attack(3), Evasion(3)),
        LegendaryGear("body-lifewell-plate", "Lifewell Plate", EquipmentKind.Body,
            "Iron on the outside. Spring water within.",
            Defense(7), Attack(1), Evasion(4)),

        Gear("feet-common", "Worn Boots", EquipmentKind.Feet, Rarity.Common, Evasion(2)),
        Gear("feet-rare", "Ranger Boots", EquipmentKind.Feet, Rarity.Rare, Evasion(4)),
        Gear("feet-epic", "Shadowstep Boots", EquipmentKind.Feet, Rarity.Epic, Evasion(6), Defense(1)),
        LegendaryGear("feet-riftwalkers", "Riftwalkers", EquipmentKind.Feet,
            "Some steps take you nowhere. That's the point.",
            Evasion(8), Defense(2), Attack(1)),
        LegendaryGear("feet-ashstriders", "Ashstriders", EquipmentKind.Feet,
            "Leave the floor warmer than you found it.",
            Evasion(7), Attack(2), Defense(2)),
        LegendaryGear("feet-galeheels", "Galeheels", EquipmentKind.Feet,
            "Faster when it matters most.",
            Evasion(9), Defense(1), Attack(1)),

        Gear("ring-common", "Copper Band", EquipmentKind.Ring, Rarity.Common, Attack(1)),
        Gear("ring-rare", "Silver Band", EquipmentKind.Ring, Rarity.Rare, Defense(2)),
        Gear("ring-epic", "Ring of Agility", EquipmentKind.Ring, Rarity.Epic, Evasion(5), Attack(1)),
        LegendaryGear("ring-band-of-second-dawn", "Band of Second Dawn", EquipmentKind.Ring,
            "Not today. Ask again tomorrow.",
            Defense(3), Evasion(3), Attack(1)),
        LegendaryGear("ring-ouroboros", "Ouroboros Ring", EquipmentKind.Ring,
            "What heals, heals again.",
            Defense(2), Attack(2), Evasion(2)),
        LegendaryGear("ring-gilded-grasp", "Gilded Grasp", EquipmentKind.Ring,
            "Greed is just ambition with better jewelry.",
            Attack(3), Defense(1), Evasion(2)),

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

    private static WeaponDefinition LegendaryWeapon(string id, string name, WeaponType type, string description, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Legendary, type, modifiers, description);

    private static GearDefinition Gear(string id, string name, EquipmentKind kind, Rarity rarity, params StatModifier[] modifiers) =>
        new(id, name, rarity, kind, modifiers);

    private static GearDefinition LegendaryGear(string id, string name, EquipmentKind kind, string description, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Legendary, kind, modifiers, description);
}