using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Content;

public static class ItemCatalog
{
    public static IReadOnlyList<ItemDefinition> All { get; } =
    [
        Weapon("sword-common", "Rusty Sword", WeaponType.Sword, Rarity.Common, Attack(3)),
        Weapon("sword-militia", "Militia Blade", WeaponType.Sword, Rarity.Common, Attack(2), Defense(1)),
        Weapon("sword-rare", "Steel Sword", WeaponType.Sword, Rarity.Rare, Attack(5)),
        Weapon("sword-falchion", "Falchion", WeaponType.Sword, Rarity.Rare, Attack(4), Critical(5)),
        Weapon("sword-epic", "Runed Blade", WeaponType.Sword, Rarity.Epic, Attack(8), Defense(1)),
        Weapon("sword-moonsteel", "Moonsteel Saber", WeaponType.Sword, Rarity.Epic, Attack(7), Evasion(4)),
        LegendaryWeapon("sword-vaultbreaker", "Vaultbreaker", WeaponType.Sword,
            "Every vault has a weakness. This blade found most of them.",
            Attack(12), Defense(2), Evasion(5)),
        LegendaryWeapon("sword-twinfang", "Twinfang", WeaponType.Sword,
            "One strike for the wound, one for the lesson. Drinks a little of every wound.",
            Attack(11), Evasion(6), Defense(1)) with { Effects = [Effect(EffectKind.LifeSteal, 10)] },
        LegendaryWeapon("sword-ember-oath", "Ember Oath", WeaponType.Sword,
            "Sworn in fire, kept in ash. Its flames catch more often and burn hotter.",
            Element.Fire, Attack(13), Defense(1), Evasion(2)) with { Effects = [Effect(EffectKind.FireBoost, 10)] },

        Weapon("dagger-common", "Rusty Dagger", WeaponType.Dagger, Rarity.Common, Attack(2)),
        Weapon("dagger-shiv", "Shiv", WeaponType.Dagger, Rarity.Common, Attack(1), Critical(5)),
        Weapon("dagger-rare", "Steel Dagger", WeaponType.Dagger, Rarity.Rare, Attack(4)),
        Weapon("dagger-stiletto", "Stiletto", WeaponType.Dagger, Rarity.Rare, Attack(3), Critical(8)),
        Weapon("dagger-epic", "Shadow Kris", WeaponType.Dagger, Rarity.Epic, Attack(6), Critical(5)),
        Weapon("dagger-venomfang", "Venomfang", WeaponType.Dagger, Rarity.Epic, Attack(5), Critical(6), Evasion(3)),
        LegendaryWeapon("dagger-whisperthorn", "Whisperthorn", WeaponType.Dagger,
            "By the time they hear it, it has already left.",
            Attack(9), Critical(15), Evasion(4)),
        LegendaryWeapon("dagger-last-breath", "Last Breath", WeaponType.Dagger,
            "Every fight ends with one. This blade just gets there first.",
            Attack(10), Critical(12), Defense(1)),
        LegendaryWeapon("dagger-gutterstar", "Gutterstar", WeaponType.Dagger,
            "Born in the alleys, polished by the stars.",
            Attack(8), Critical(14), Evasion(6)),

        Weapon("axe-common", "Hatchet", WeaponType.Axe, Rarity.Common, Attack(4)),
        Weapon("axe-woodcutter", "Woodcutter's Axe", WeaponType.Axe, Rarity.Common, Attack(3), Defense(1)),
        Weapon("axe-rare", "Battle Axe", WeaponType.Axe, Rarity.Rare, Attack(6)),
        Weapon("axe-bearded", "Bearded Axe", WeaponType.Axe, Rarity.Rare, Attack(5), Defense(2)),
        Weapon("axe-epic", "Bonecleaver", WeaponType.Axe, Rarity.Epic, Attack(9), Defense(2)),
        Weapon("axe-executioner", "Executioner", WeaponType.Axe, Rarity.Epic, Attack(10), Critical(3)),
        LegendaryWeapon("axe-riftsplitter", "Riftsplitter", WeaponType.Axe,
            "The rift was here first. The axe just widened it.",
            Attack(13), Defense(3), Evasion(3)),
        LegendaryWeapon("axe-grudgebearer", "Grudgebearer", WeaponType.Axe,
            "It remembers every blow, and returns a little of each.",
            Attack(12), Defense(4), Evasion(2)) with { Effects = [Effect(EffectKind.Thorns, 2)] },
        LegendaryWeapon("axe-stormmaw", "Stormmaw", WeaponType.Axe,
            "Thunder doesn't ask permission. Its lightning jumps twice.",
            Element.Lightning, Attack(14), Defense(2), Evasion(2)) with { Effects = [Effect(EffectKind.LightningBoost, 10)] },

        Weapon("bow-common", "Short Bow", WeaponType.Bow, Rarity.Common, Attack(3)),
        Weapon("bow-hunting", "Hunting Bow", WeaponType.Bow, Rarity.Common, Attack(2), Evasion(2)),
        Weapon("bow-rare", "Hunter's Bow", WeaponType.Bow, Rarity.Rare, Attack(5)),
        Weapon("bow-recurve", "Recurve Bow", WeaponType.Bow, Rarity.Rare, Attack(4), Critical(5)),
        Weapon("bow-epic", "Whisperwind", WeaponType.Bow, Rarity.Epic, Attack(8), Evasion(5)),
        Weapon("bow-longbow", "Marcher's Longbow", WeaponType.Bow, Rarity.Epic, Attack(9), Critical(2)),
        LegendaryWeapon("bow-starshot", "Starshot", WeaponType.Bow,
            "Aim for one. Hit the one behind him too. Reaches two tiles farther.",
            Attack(12), Evasion(8), Defense(1)) with { Effects = [Effect(EffectKind.BowRange, 2)] },
        LegendaryWeapon("bow-nightglass", "Nightglass", WeaponType.Bow,
            "Sleep is just a slower kind of surrender. Its frost holds longer.",
            Element.Ice, Attack(13), Evasion(6), Defense(1)) with { Effects = [Effect(EffectKind.IceBoost, 10)] },
        LegendaryWeapon("bow-galecaller", "Galecaller", WeaponType.Bow,
            "Ask the wind nicely. Then pull. Its gusts push twice as far.",
            Element.Wind, Attack(11), Evasion(10), Defense(1)) with { Effects = [Effect(EffectKind.WindBoost, 10)] },

        Weapon("spear-common", "Wooden Spear", WeaponType.Spear, Rarity.Common, Attack(4)),
        Weapon("spear-boar", "Boar Spear", WeaponType.Spear, Rarity.Common, Attack(3), Defense(1)),
        Weapon("spear-rare", "Iron Pike", WeaponType.Spear, Rarity.Rare, Attack(6)),
        Weapon("spear-partisan", "Partisan", WeaponType.Spear, Rarity.Rare, Attack(5), Evasion(3)),
        Weapon("spear-epic", "Dragoon Lance", WeaponType.Spear, Rarity.Epic, Attack(9), Defense(2)),
        Weapon("spear-glaive", "Glaive", WeaponType.Spear, Rarity.Epic, Attack(8), Critical(6)),
        LegendaryWeapon("spear-skypiercer", "Skypiercer", WeaponType.Spear,
            "Thrown once at the sky. The sky has not been the same since.",
            Attack(13), Evasion(4), Critical(5)),
        LegendaryWeapon("spear-tidebreaker", "Tidebreaker", WeaponType.Spear,
            "Cold as the deep water it was forged under.",
            Element.Ice, Attack(12), Defense(3), Evasion(2)),
        LegendaryWeapon("spear-sunlance", "Sunlance", WeaponType.Spear,
            "Noon, sharpened to a point.",
            Element.Fire, Attack(14), Defense(1), Critical(3)),

        Gear("shield-buckler", "Buckler", EquipmentKind.Shield, Rarity.Common, Defense(1), Block(10)),
        Gear("shield-round", "Round Shield", EquipmentKind.Shield, Rarity.Common, Defense(2), Block(8)),
        Gear("shield-kite", "Kite Shield", EquipmentKind.Shield, Rarity.Rare, Defense(2), Block(15)),
        Gear("shield-spiked", "Spiked Buckler", EquipmentKind.Shield, Rarity.Rare, Defense(1), Block(12))
            with { Effects = [Effect(EffectKind.Thorns, 1)] },
        Gear("shield-tower", "Tower Shield", EquipmentKind.Shield, Rarity.Epic, Defense(4), Block(18)),
        Gear("shield-warden", "Warden's Aegis", EquipmentKind.Shield, Rarity.Epic, Defense(3), Block(20)),
        LegendaryGear("shield-dawnwall", "Dawnwall", EquipmentKind.Shield,
            "It has stood in front of every sunrise. Heals the one behind it.",
            Defense(5), Block(25)) with { Effects = [Effect(EffectKind.Regeneration, 1)] },
        LegendaryGear("shield-mirrorwall", "Mirrorwall", EquipmentKind.Shield,
            "Whatever you bring, it gives back.",
            Defense(4), Block(25)) with { Effects = [Effect(EffectKind.Thorns, 3)] },
        LegendaryGear("shield-stormguard", "Stormguard", EquipmentKind.Shield,
            "Hold the line a little longer. Your abilities return sooner.",
            Defense(4), Block(22), Evasion(2)) with { Effects = [Effect(EffectKind.Haste, 2)] },

        Gear("head-common", "Leather Cap", EquipmentKind.Head, Rarity.Common, Defense(1)),
        Gear("head-rare", "Iron Helm", EquipmentKind.Head, Rarity.Rare, Defense(2)),
        Gear("head-epic", "Warden's Visage", EquipmentKind.Head, Rarity.Epic, Defense(3), Evasion(3)),
        LegendaryGear("head-crown-of-echoes", "Crown of Echoes", EquipmentKind.Head,
            "Dodge once. Strike twice as true.",
            Defense(4), Evasion(5), Attack(1)),
        LegendaryGear("head-mask-of-the-hollow", "Mask of the Hollow", EquipmentKind.Head,
            "Wear no face, wake no one. Monsters notice you one tile later.",
            Defense(4), Evasion(4), Attack(2)) with { Effects = [Effect(EffectKind.Stealth, 1)] },
        LegendaryGear("head-seers-visor", "Seer's Visor", EquipmentKind.Head,
            "The dark has edges. Now you can see them, and what they hide.",
            Defense(5), Evasion(3), Attack(1)) with { Effects = [Effect(EffectKind.Luck, 25)] },

        Gear("body-common", "Leather Vest", EquipmentKind.Body, Rarity.Common, Defense(1)),
        Gear("body-rare", "Chainmail", EquipmentKind.Body, Rarity.Rare, Defense(2)),
        Gear("body-epic", "Warden's Plate", EquipmentKind.Body, Rarity.Epic, Defense(4), Attack(1)),
        LegendaryGear("body-aegis-of-the-rift", "Aegis of the Rift", EquipmentKind.Body,
            "The first blow is a question. The Aegis answers no.",
            Defense(6), Attack(2), Evasion(10)),
        LegendaryGear("body-bramblehide", "Bramblehide", EquipmentKind.Body,
            "Touch it and it touches back.",
            Defense(6), Attack(3), Evasion(3)) with { Effects = [Effect(EffectKind.Thorns, 2)] },
        LegendaryGear("body-lifewell-plate", "Lifewell Plate", EquipmentKind.Body,
            "Iron on the outside. Spring water within.",
            Defense(7), Attack(1), Evasion(4)) with { Effects = [Effect(EffectKind.Regeneration, 1)] },

        Gear("feet-common", "Worn Boots", EquipmentKind.Feet, Rarity.Common, Evasion(2)),
        Gear("feet-rare", "Ranger Boots", EquipmentKind.Feet, Rarity.Rare, Evasion(4)),
        Gear("feet-epic", "Shadowstep Boots", EquipmentKind.Feet, Rarity.Epic, Evasion(6), Defense(1)),
        LegendaryGear("feet-riftwalkers", "Riftwalkers", EquipmentKind.Feet,
            "Some steps take you nowhere. That's the point.",
            Evasion(8), Defense(2), Attack(1)),
        LegendaryGear("feet-ashstriders", "Ashstriders", EquipmentKind.Feet,
            "Leave the floor warmer than you found it.",
            Evasion(7), Attack(2), Defense(2)) with { Effects = [Effect(EffectKind.FireBoost, 5)] },
        LegendaryGear("feet-galeheels", "Galeheels", EquipmentKind.Feet,
            "Faster when it matters most.",
            Evasion(9), Defense(1), Attack(1)) with { Effects = [Effect(EffectKind.Haste, 1)] },

        Gear("ring-common", "Copper Band", EquipmentKind.Ring, Rarity.Common, Attack(1)),
        Gear("ring-rare", "Silver Band", EquipmentKind.Ring, Rarity.Rare, Defense(2)),
        Gear("ring-epic", "Ring of Agility", EquipmentKind.Ring, Rarity.Epic, Evasion(5), Attack(1)),
        Ring("ring-ember", "Ember Ring", Rarity.Rare, Effect(EffectKind.FireBoost, 15), Attack(1)),
        Ring("ring-frost", "Frost Ring", Rarity.Rare, Effect(EffectKind.IceBoost, 15), Defense(1)),
        Ring("ring-storm", "Storm Ring", Rarity.Rare, Effect(EffectKind.LightningBoost, 15), Evasion(2)),
        Ring("ring-gale", "Gale Ring", Rarity.Rare, Effect(EffectKind.WindBoost, 15), Evasion(2)),
        Ring("ring-thorns", "Thorn Ring", Rarity.Rare, Effect(EffectKind.Thorns, 2), Defense(1)),
        Ring("ring-hunter", "Hunter's Ring", Rarity.Rare, Effect(EffectKind.BowRange, 2), Critical(2)),
        Ring("ring-flow", "Ring of Flow", Rarity.Rare, Effect(EffectKind.ManaFlow, 1), Mana(5)),
        Ring("ring-vampire", "Vampire Ring", Rarity.Epic, Effect(EffectKind.LifeSteal, 15), Attack(1)),
        Ring("ring-fortune", "Ring of Fortune", Rarity.Epic, Effect(EffectKind.Luck, 50)),
        Ring("ring-sage", "Sage's Ring", Rarity.Epic, Effect(EffectKind.Wisdom, 20)),
        Ring("ring-time", "Ring of Time", Rarity.Epic, Effect(EffectKind.Haste, 2)),
        LegendaryGear("ring-band-of-second-dawn", "Band of Second Dawn", EquipmentKind.Ring,
            "Not today. Ask again tomorrow. Your abilities come back sooner.",
            Defense(3), Evasion(3), Attack(1)) with { Effects = [Effect(EffectKind.Haste, 3)] },
        LegendaryGear("ring-ouroboros", "Ouroboros Ring", EquipmentKind.Ring,
            "What heals, heals again.",
            Defense(2), Attack(2), Evasion(2)) with { Effects = [Effect(EffectKind.LifeSteal, 20)] },
        LegendaryGear("ring-gilded-grasp", "Gilded Grasp", EquipmentKind.Ring,
            "Greed is just ambition with better jewelry.",
            Attack(3), Defense(1), Evasion(2)) with { Effects = [Effect(EffectKind.Luck, 75)] },

        Gear("cloak-common", "Traveler's Cloak", EquipmentKind.Cloak, Rarity.Common, Evasion(1)),
        Gear("cloak-rare", "Hooded Cloak", EquipmentKind.Cloak, Rarity.Rare, Evasion(2)),
        Gear("cloak-epic", "Shadow Mantle", EquipmentKind.Cloak, Rarity.Epic, Evasion(3), Defense(1)),
        LegendaryGear("cloak-cinderweave", "Cinderweave", EquipmentKind.Cloak,
            "Woven from embers that refused to go out.",
            Element.Fire, Evasion(5), Defense(2), Attack(1)),
        LegendaryGear("cloak-rimeveil", "Rimeveil", EquipmentKind.Cloak,
            "Winter wears it to look warmer.",
            Element.Ice, Evasion(5), Defense(2), Attack(1)),
        LegendaryGear("cloak-stormshroud", "Stormshroud", EquipmentKind.Cloak,
            "The sky lent it a storm and never asked for it back.",
            Element.Lightning, Evasion(6), Defense(1), Attack(1)),

        ClassMod("mod-knight-sentinel", "Sentinel", "knight", Rarity.Rare,
            "Block 10% more often. Bulwark lasts one more turn.",
            [Effect(EffectKind.BulwarkExtension, 1)], Block(10), Defense(1)),
        ClassMod("mod-knight-vigor", "Vigor", "knight", Rarity.Epic,
            "Regenerate 1 health every turn.",
            [Effect(EffectKind.Regeneration, 1)], Defense(2)),
        ClassMod("mod-knight-avenger", "Avenger", "knight", Rarity.Legendary,
            "A blocked blow hits the attacker instead.",
            [Effect(EffectKind.BlockReflect, 1)], Block(5), Defense(2)),
        ClassMod("mod-berserker-bloodthirst", "Bloodthirst", "berserker", Rarity.Rare,
            "During Fury, heal 20% of the damage you deal.",
            [Effect(EffectKind.FuryLifeSteal, 20)], Attack(1)),
        ClassMod("mod-berserker-raging-mend", "Raging Mend", "berserker", Rarity.Epic,
            "Regenerate 1 health every turn, 2 during Fury.",
            [Effect(EffectKind.Regeneration, 1), Effect(EffectKind.FuryRegeneration, 1)], Defense(1)),
        ClassMod("mod-berserker-slaughterer", "Slaughterer", "berserker", Rarity.Legendary,
            "Every kill during Fury makes it last one more turn.",
            [Effect(EffectKind.FuryExtension, 1)], Attack(3)),
        ClassMod("mod-ranger-hawkeye", "Hawkeye", "ranger", Rarity.Rare,
            "Your bow reaches two tiles farther.",
            [Effect(EffectKind.BowRange, 2)], Critical(2)),
        ClassMod("mod-ranger-sniper", "Sniper", "ranger", Rarity.Epic,
            "+15% critical against targets 4 or more tiles away.",
            [Effect(EffectKind.FarShotCritical, 15)], Attack(1)),
        ClassMod("mod-ranger-volley", "Volley", "ranger", Rarity.Legendary,
            "Power Shot recharges twice as fast and always triggers your bow's element.",
            [Effect(EffectKind.PowerShotMastery, 1)], Attack(2)),
        ClassMod("mod-rogue-shadow", "Shadow", "rogue", Rarity.Rare,
            "Monsters wake up only when you are 3 tiles away.",
            [Effect(EffectKind.Stealth, 2)], Evasion(3)),
        ClassMod("mod-rogue-assassin", "Assassin", "rogue", Rarity.Epic,
            "Striking from invisibility deals triple damage.",
            [Effect(EffectKind.Ambush, 1)], Critical(5)),
        ClassMod("mod-rogue-phantom", "Phantom", "rogue", Rarity.Legendary,
            "Invisibility lasts 2 more turns, and a killing blow keeps you hidden.",
            [Effect(EffectKind.Phantom, 2)], Evasion(5)),

        new PotionDefinition("potion-healing", "Healing Potion", Rarity.Common, Healing: 10),
        new PotionDefinition("potion-greater-healing", "Greater Healing Potion", Rarity.Rare, Healing: 25),
        new PotionDefinition("potion-mana", "Mana Potion", Rarity.Common, ManaRestore: 10),
        new PotionDefinition("potion-greater-mana", "Greater Mana Potion", Rarity.Rare, ManaRestore: 25),
        new PotionDefinition("potion-strength", "Strength Potion", Rarity.Rare, Boost: Attack(3), BoostTurns: 10),
        new PotionDefinition("potion-protection", "Protection Potion", Rarity.Rare, Boost: Defense(3), BoostTurns: 10)
    ];

    public static IReadOnlyList<EquipmentDefinition> Equipment { get; } =
        All.OfType<EquipmentDefinition>().Where(item => item.Kind != EquipmentKind.ClassMod).ToList();

    public static IReadOnlyList<GearDefinition> ClassMods { get; } =
        All.OfType<GearDefinition>().Where(item => item.Kind == EquipmentKind.ClassMod).ToList();

    public static ItemDefinition Get(string id) =>
        All.FirstOrDefault(item => item.Id == id)
        ?? throw new KeyNotFoundException($"No item with id '{id}' exists in the catalog.");

    private static StatModifier Attack(int amount) => new(StatType.Attack, amount);

    private static StatModifier Defense(int amount) => new(StatType.Defense, amount);

    private static StatModifier Evasion(int amount) => new(StatType.Evasion, amount);

    private static StatModifier Critical(int amount) => new(StatType.Critical, amount);

    private static StatModifier Block(int amount) => new(StatType.Block, amount);

    private static StatModifier Mana(int amount) => new(StatType.Mana, amount);

    private static ItemEffect Effect(EffectKind kind, int amount) => new(kind, amount);

    private static WeaponDefinition Weapon(string id, string name, WeaponType type, Rarity rarity, params StatModifier[] modifiers) =>
        new(id, name, rarity, type, modifiers);

    private static WeaponDefinition LegendaryWeapon(string id, string name, WeaponType type, string description, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Legendary, type, modifiers, description);

    private static WeaponDefinition LegendaryWeapon(string id, string name, WeaponType type, string description, Element element, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Legendary, type, modifiers, description, element);

    private static GearDefinition Gear(string id, string name, EquipmentKind kind, Rarity rarity, params StatModifier[] modifiers) =>
        new(id, name, rarity, kind, modifiers);

    private static GearDefinition Ring(string id, string name, Rarity rarity, ItemEffect effect, params StatModifier[] modifiers) =>
        new(id, name, rarity, EquipmentKind.Ring, modifiers) { Effects = [effect] };

    private static GearDefinition ClassMod(
        string id,
        string name,
        string classId,
        Rarity rarity,
        string description,
        ItemEffect[] effects,
        params StatModifier[] modifiers) =>
        new(id, name, rarity, EquipmentKind.ClassMod, modifiers, description) { ClassId = classId, Effects = effects };

    private static GearDefinition LegendaryGear(string id, string name, EquipmentKind kind, string description, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Legendary, kind, modifiers, description);

    private static GearDefinition LegendaryGear(string id, string name, EquipmentKind kind, string description, Element element, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Legendary, kind, modifiers, description, element);
}
