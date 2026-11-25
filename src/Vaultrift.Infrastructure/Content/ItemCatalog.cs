using Vaultrift.Engine.Gameplay;
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
        Weapon("sword-duelist", "Duelist's Rapier", WeaponType.Sword, Rarity.Epic, Attack(7), Critical(6), Evasion(3)),
        Weapon("sword-knightguard", "Knightguard Blade", WeaponType.Sword, Rarity.Epic, Attack(8), Defense(3)),
        LegendaryWeapon("sword-dawnbringer", "Dawnbringer", WeaponType.Sword,
            "First light finds every wound, and closes yours.",
            Attack(12), Defense(2), Evasion(3)) with { Effects = [Effect(EffectKind.Regeneration, 1)] },
        LegendaryWeapon("sword-frostbite", "Frostbite Edge", WeaponType.Sword,
            "Cold enough to stop a heartbeat. Its frost holds longer.",
            Element.Ice, Attack(13), Evasion(3), Defense(1)) with { Effects = [Effect(EffectKind.IceBoost, 10)] },

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
        Weapon("dagger-viper", "Viper's Tooth", WeaponType.Dagger, Rarity.Epic, Attack(6), Critical(8)),
        Weapon("dagger-smokeblade", "Smokeblade", WeaponType.Dagger, Rarity.Epic, Attack(5), Evasion(6), Critical(4)),
        LegendaryWeapon("dagger-moonshiver", "Moonshiver", WeaponType.Dagger,
            "It hums before a storm. Its lightning jumps twice.",
            Element.Lightning, Attack(9), Critical(12), Evasion(3)) with { Effects = [Effect(EffectKind.LightningBoost, 10)] },
        LegendaryWeapon("dagger-bloodletter", "Bloodletter", WeaponType.Dagger,
            "Every cut pays the wielder back.",
            Attack(9), Critical(13), Evasion(2)) with { Effects = [Effect(EffectKind.LifeSteal, 10)] },

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
        Weapon("axe-ironbark", "Ironbark Axe", WeaponType.Axe, Rarity.Epic, Attack(9), Defense(3)),
        Weapon("axe-ravager", "Ravager", WeaponType.Axe, Rarity.Epic, Attack(10), Critical(4)),
        LegendaryWeapon("axe-cinderhowl", "Cinderhowl", WeaponType.Axe,
            "It howls when it swings, and the embers answer. Its flames catch more often and burn hotter.",
            Element.Fire, Attack(14), Defense(2), Evasion(1)) with { Effects = [Effect(EffectKind.FireBoost, 10)] },
        LegendaryWeapon("axe-oakheart", "Oakheart", WeaponType.Axe,
            "Cut from a tree that refused to fall. Mends its wielder slowly.",
            Attack(12), Defense(5), Evasion(1)) with { Effects = [Effect(EffectKind.Regeneration, 1)] },

        Weapon("bow-common", "Short Bow", WeaponType.Bow, Rarity.Common, Attack(3)),
        Weapon("bow-hunting", "Hunting Bow", WeaponType.Bow, Rarity.Common, Attack(2), Evasion(2)),
        Weapon("bow-rare", "Hunter's Bow", WeaponType.Bow, Rarity.Rare, Attack(5)),
        Weapon("bow-recurve", "Recurve Bow", WeaponType.Bow, Rarity.Rare, Attack(4), Critical(5)),
        Weapon("bow-epic", "Whisperwind", WeaponType.Bow, Rarity.Epic, Attack(8), Evasion(5)),
        Weapon("bow-longbow", "Marcher's Longbow", WeaponType.Bow, Rarity.Epic, Attack(9), Critical(2)),
        LegendaryWeapon("bow-starshot", "Starshot", WeaponType.Bow,
            "Aim for one. The second arrow is already on its way.",
            Attack(12), Evasion(8), Defense(1)) with { Effects = [Effect(EffectKind.DoubleShot, 25)] },
        LegendaryWeapon("bow-nightglass", "Nightglass", WeaponType.Bow,
            "Sleep is just a slower kind of surrender. Its frost holds longer.",
            Element.Ice, Attack(13), Evasion(6), Defense(1)) with { Effects = [Effect(EffectKind.IceBoost, 10)] },
        LegendaryWeapon("bow-galecaller", "Galecaller", WeaponType.Bow,
            "Ask the wind nicely. Then pull. Its gusts push twice as far.",
            Element.Wind, Attack(11), Evasion(10), Defense(1)) with { Effects = [Effect(EffectKind.WindBoost, 10)] },
        Weapon("bow-thornwood", "Thornwood Bow", WeaponType.Bow, Rarity.Epic, Attack(8), Critical(5)),
        Weapon("bow-falconer", "Falconer's Bow", WeaponType.Bow, Rarity.Epic, Attack(7), Evasion(6)),
        LegendaryWeapon("bow-sunpiercer", "Sunpiercer", WeaponType.Bow,
            "Each arrow carries a piece of noon. Its flames catch more often and burn hotter.",
            Element.Fire, Attack(13), Evasion(5), Critical(2)) with { Effects = [Effect(EffectKind.FireBoost, 10)] },
        LegendaryWeapon("bow-hawkeye", "Hawkeye", WeaponType.Bow,
            "Farther is better. +15% critical against targets 4 or more tiles away.",
            Attack(12), Evasion(7), Critical(3)) with { Effects = [Effect(EffectKind.FarShotCritical, 15)] },

        Staff("staff-oak", "Oak Staff", Rarity.Common, SpellCatalog.FireBolt, Attack(3)),
        Staff("staff-ash", "Ash Wand", Rarity.Common, SpellCatalog.IceShard, Attack(2), Mana(5)),
        Staff("staff-copper", "Copper Rod", Rarity.Rare, SpellCatalog.Spark, Attack(5)),
        Staff("staff-willow", "Willow Staff", Rarity.Rare, SpellCatalog.Gust, Attack(4), Mana(5)),
        Staff("staff-emberwood", "Emberwood Staff", Rarity.Epic, SpellCatalog.FireBolt, Attack(8), Mana(5)),
        Staff("staff-frostspire", "Frostspire", Rarity.Epic, SpellCatalog.IceShard, Attack(7), Mana(10)),
        LegendaryStaff("staff-riftheart", "Riftheart", SpellCatalog.Spark,
            "A shard of the rift on a stick. Spells cost one less mana and catch more often.",
            [Effect(EffectKind.SpellMastery, 15)], Attack(11), Mana(10)),
        LegendaryStaff("staff-everflame", "Everflame", SpellCatalog.FireBolt,
            "It has never gone out. Its fire catches more often and burns hotter.",
            [Effect(EffectKind.FireBoost, 10)], Attack(12), Mana(5)),
        LegendaryStaff("staff-whirlwind", "Whirlwind Crook", SpellCatalog.Gust,
            "The shepherd of storms. Mana flows back faster.",
            [Effect(EffectKind.ManaFlow, 1)], Attack(11), Mana(10)),
        Staff("staff-stormglass", "Stormglass Rod", Rarity.Epic, SpellCatalog.Spark, Attack(8), Mana(5)),
        Staff("staff-galebranch", "Gale Branch", Rarity.Epic, SpellCatalog.Gust, Attack(7), Mana(10)),
        LegendaryStaff("staff-glacierheart", "Glacier Heart", SpellCatalog.IceShard,
            "A heart of old ice that never thaws. Its frost holds longer.",
            [Effect(EffectKind.IceBoost, 10)], Attack(12), Mana(10)),
        LegendaryStaff("staff-sages-lantern", "Sage's Lantern", SpellCatalog.Spark,
            "It lights the way and drinks the dark. Every kill gives back 5 mana.",
            [Effect(EffectKind.ManaSiphon, 5)], Attack(11), Mana(15)),

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
        Weapon("spear-halberd", "Halberd", WeaponType.Spear, Rarity.Epic, Attack(10), Defense(2)),
        Weapon("spear-wyrmfang", "Wyrmfang Spear", WeaponType.Spear, Rarity.Epic, Attack(9), Critical(5)),
        LegendaryWeapon("spear-stormcaller", "Stormcaller Pike", WeaponType.Spear,
            "Raise it and the sky listens. Its lightning jumps twice.",
            Element.Lightning, Attack(13), Evasion(3), Defense(1)) with { Effects = [Effect(EffectKind.LightningBoost, 10)] },
        LegendaryWeapon("spear-zephyr", "Zephyr's Reach", WeaponType.Spear,
            "Light as a breeze, sharp as a gale. Its gusts push twice as far.",
            Element.Wind, Attack(12), Evasion(6)) with { Effects = [Effect(EffectKind.WindBoost, 10)] },

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
            Defense(4), Block(22), Evasion(2)) with { Effects = [Effect(EffectKind.Haste, 15)] },

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
            Evasion(9), Defense(1), Attack(1)) with { Effects = [Effect(EffectKind.Haste, 10)] },

        Gear("ring-common", "Copper Band", EquipmentKind.Ring, Rarity.Common, Attack(1)),
        Gear("ring-rare", "Silver Band", EquipmentKind.Ring, Rarity.Rare, Defense(2)),
        Gear("ring-epic", "Ring of Agility", EquipmentKind.Ring, Rarity.Epic, Evasion(5), Attack(1)),
        Ring("ring-ember", "Ember Ring", Rarity.Rare, Effect(EffectKind.FireBoost, 15), Attack(1)),
        Ring("ring-frost", "Frost Ring", Rarity.Rare, Effect(EffectKind.IceBoost, 15), Defense(1)),
        Ring("ring-storm", "Storm Ring", Rarity.Rare, Effect(EffectKind.LightningBoost, 15), Evasion(2)),
        Ring("ring-gale", "Gale Ring", Rarity.Rare, Effect(EffectKind.WindBoost, 15), Evasion(2)),
        Ring("ring-thorns", "Thorn Ring", Rarity.Rare, Effect(EffectKind.Thorns, 2), Defense(1)),
        Ring("ring-hunter", "Hunter's Ring", Rarity.Rare, Effect(EffectKind.DoubleShot, 15), Critical(2)),
        Ring("ring-flow", "Ring of Flow", Rarity.Rare, Effect(EffectKind.ManaFlow, 1), Mana(5)),
        Ring("ring-vampire", "Vampire Ring", Rarity.Epic, Effect(EffectKind.LifeSteal, 15), Attack(1)),
        Ring("ring-fortune", "Ring of Fortune", Rarity.Epic, Effect(EffectKind.Luck, 50)),
        Ring("ring-sage", "Sage's Ring", Rarity.Epic, Effect(EffectKind.Wisdom, 20)),
        Ring("ring-time", "Ring of Time", Rarity.Epic, Effect(EffectKind.Haste, 15)),
        LegendaryGear("ring-band-of-second-dawn", "Band of Second Dawn", EquipmentKind.Ring,
            "Not today. Ask again tomorrow. Your abilities come back sooner.",
            Defense(3), Evasion(3), Attack(1)) with { Effects = [Effect(EffectKind.Haste, 20)] },
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
            "One arrow in five flies twice.",
            [Effect(EffectKind.DoubleShot, 20)], Critical(2)),
        ClassMod("mod-ranger-sniper", "Sniper", "ranger", Rarity.Epic,
            "+15% critical against targets 4 or more tiles away.",
            [Effect(EffectKind.FarShotCritical, 15)], Attack(1)),
        ClassMod("mod-ranger-volley", "Volley", "ranger", Rarity.Legendary,
            "Power Shot recharges 30% faster and always triggers your bow's element.",
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
        ClassMod("mod-mage-archmage", "Archmage", "mage", Rarity.Rare,
            "A deeper well: 20 more mana, and it flows back faster.",
            [Effect(EffectKind.ManaFlow, 1)], Mana(20)),
        ClassMod("mod-mage-siphon", "Siphon", "mage", Rarity.Epic,
            "Every kill gives back 5 mana.",
            [Effect(EffectKind.ManaSiphon, 5)], Attack(1)),
        ClassMod("mod-mage-elementalist", "Elementalist", "mage", Rarity.Legendary,
            "Spells cost one less mana and their element catches 20% more often.",
            [Effect(EffectKind.SpellMastery, 20)], Attack(2), Mana(10)),

        new PotionDefinition("potion-healing", "Healing Potion", Rarity.Common, Healing: 10),
        new PotionDefinition("potion-greater-healing", "Greater Healing Potion", Rarity.Rare, Healing: 25),
        new PotionDefinition("potion-mana", "Mana Potion", Rarity.Common, ManaRestore: 10),
        new PotionDefinition("potion-greater-mana", "Greater Mana Potion", Rarity.Rare, ManaRestore: 25),
        new PotionDefinition("potion-strength", "Strength Potion", Rarity.Rare, Boost: Attack(3), BoostTurns: 10),
        new PotionDefinition("potion-protection", "Protection Potion", Rarity.Rare, Boost: Defense(3), BoostTurns: 10),

        Scroll("fireball", Rarity.Rare, "Explodes where it lands, burning everything around."),
        Scroll("frost-nova", Rarity.Rare, "A ring of frost that freezes every enemy next to you."),
        Scroll("chain-lightning", Rarity.Rare, "Leaps from enemy to enemy, up to three of them."),
        Scroll("squall", Rarity.Rare, "A blast down the whole row that hurls enemies two tiles back."),
        Scroll("fire-rain", Rarity.Epic, "Fire falls on a 5×5 area and sets every enemy alight."),
        Scroll("blizzard", Rarity.Epic, "A storm of ice two tiles around you."),
        Scroll("tempest", Rarity.Epic, "Lightning strikes every enemy within five tiles."),
        Scroll("tornado", Rarity.Epic, "A whirlwind where it lands that drags enemies to its heart."),

        Exotic("sword-riftrender", "Riftrender", WeaponType.Sword, ExoticPower.Riftrender,
            "It remembers where it cut last. Every third blow in a row on the same foe tears through the rift: double damage that ignores defense.",
            Attack(15), Critical(5), Evasion(3)),
        Exotic("sword-oathbound", "Oathbound", WeaponType.Sword, ExoticPower.Oathbound,
            "Every wound is a promise. Your next attack adds all the melee damage you took since your last one.",
            Attack(14), Defense(4), Evasion(2)),
        Exotic("axe-headsmans-moon", "Headsman's Moon", WeaponType.Axe, ExoticPower.HeadsmansMoon,
            "The moon does not wait its turn. If your sweep kills, the monsters do not get to act.",
            Attack(16), Defense(3), Critical(3)),
        Exotic("axe-worldsplitter", "Worldsplitter", WeaponType.Axe, ExoticPower.Worldsplitter,
            "The ground remembers the blow. Every monster hit by your sweep loses its next turn (bosses keep their footing).",
            Attack(15), Defense(4), Evasion(1)),
        Exotic("bow-rift-needle", "Rift Needle", WeaponType.Bow, ExoticPower.RiftNeedle,
            "Nothing stops it. Arrows pierce every monster in line; each one after the first takes 75% damage.",
            Attack(15), Evasion(6), Critical(3)),
        Exotic("bow-huntress-promise", "Huntress' Promise", WeaponType.Bow, ExoticPower.HuntressPromise,
            "Once marked, never lost. A hit marks the target for 3 turns: it takes 50% more from your weapon, and killing it while marked heals you for 10% of your health.",
            Attack(14), Evasion(8), Critical(3)),
        Exotic("dagger-widows-kiss", "Widow's Kiss", WeaponType.Dagger, ExoticPower.WidowsKiss,
            "Slow, patient, certain. Every hit adds a layer of venom (up to 5); each layer costs the target 2 health every turn, bosses included.",
            Attack(11), Critical(14), Evasion(4)),
        Exotic("dagger-shadowstep", "Shadowstep", WeaponType.Dagger, ExoticPower.Shadowstep,
            "Distance is a suggestion. Attack a monster two tiles away with a free tile between you, and you leap next to it for a guaranteed critical hit. Recharges in 3 turns.",
            Attack(11), Critical(12), Evasion(6)),
        Exotic("spear-last-wall", "Last Wall", WeaponType.Spear, ExoticPower.LastWall,
            "Hold the line. Any monster that steps onto the tile in front of you takes a free hit.",
            Attack(16), Defense(3), Evasion(1)),
        Exotic("spear-starfall-lance", "Starfall Lance", WeaponType.Spear, ExoticPower.StarfallLance,
            "Thrown down from the sky, it still has reach to spare. Your thrust reaches 3 tiles and hits everything in line.",
            Attack(16), Critical(4), Evasion(2)),
        ExoticStaff("staff-prism-of-ages", "Prism of Ages", SpellCatalog.Spark, ExoticPower.Prism,
            "Light bends to find the crack. Your spells always strike the target's weakness, whatever their element.",
            Attack(14), Mana(15)),
        ExoticStaff("staff-echo", "Echo Staff", SpellCatalog.FireBolt, ExoticPower.Echo,
            "Every word is spoken twice. Each spell from the staff is cast a second time, free, at half power.",
            Attack(13), Mana(20)),

        new RuneDefinition("rune-fire", "Rune of Fire", Rarity.Rare, Element.Fire,
            "Use it to give the weapon in your main hand the power of fire, for good. Not for staves or weapons that already have a fixed element."),
        new RuneDefinition("rune-ice", "Rune of Ice", Rarity.Rare, Element.Ice,
            "Use it to give the weapon in your main hand the power of ice, for good. Not for staves or weapons that already have a fixed element."),
        new RuneDefinition("rune-lightning", "Rune of Lightning", Rarity.Rare, Element.Lightning,
            "Use it to give the weapon in your main hand the power of lightning, for good. Not for staves or weapons that already have a fixed element."),
        new RuneDefinition("rune-wind", "Rune of Wind", Rarity.Rare, Element.Wind,
            "Use it to give the weapon in your main hand the power of wind, for good. Not for staves or weapons that already have a fixed element.")
    ];

    public static IReadOnlyList<ScrollDefinition> Scrolls { get; } = All.OfType<ScrollDefinition>().ToList();

    public static IReadOnlyList<RuneDefinition> Runes { get; } = All.OfType<RuneDefinition>().ToList();

    public static IReadOnlyList<WeaponDefinition> Exotics { get; } =
        All.OfType<WeaponDefinition>().Where(weapon => weapon.Rarity == Rarity.Exotic).ToList();

    public static IReadOnlyList<EquipmentDefinition> Equipment { get; } =
        All.OfType<EquipmentDefinition>().Where(item => item.Kind != EquipmentKind.ClassMod).ToList();

    public static IReadOnlyList<GearDefinition> ClassMods { get; } =
        All.OfType<GearDefinition>().Where(item => item.Kind == EquipmentKind.ClassMod).ToList();

    public static IReadOnlyList<WeaponDefinition> Spares { get; } =
        new[] { "sword-common", "axe-common", "bow-common", "dagger-common", "staff-oak" }
            .Select(id => (WeaponDefinition)All.First(item => item.Id == id))
            .Select(weapon => weapon with { Id = SparePrefix + weapon.Id, Name = $"Spare {weapon.Name}", IsSpare = true })
            .ToList();

    public static WeaponDefinition Spare(string weaponId) =>
        Spares.FirstOrDefault(spare => spare.Id == SparePrefix + weaponId)
        ?? throw new KeyNotFoundException($"No spare exists for '{weaponId}'.");

    public const int DragonShardTiers = 3;

    private static readonly Element[] Elements = Enum.GetValues<Element>();

    public static IReadOnlyList<EquipmentDefinition> DragonShards { get; } =
        Enumerable.Range(1, DragonShardTiers)
            .SelectMany(tier => new EquipmentDefinition[]
            {
                DragonAxes(tier),
                DragonBlade(tier),
                DragonAegis(tier),
                DragonBow(tier),
                DragonFangs(tier)
            }.Concat(Elements.Select(element => DragonStaff(tier, element))))
            .ToList();

    public static IReadOnlyList<EquipmentDefinition> DragonShardSet(string? classId, int tier, Element element)
    {
        var level = Math.Clamp(tier, 1, DragonShardTiers);
        EquipmentDefinition Find(string id) => DragonShards.First(item => item.Id == id);

        return classId switch
        {
            "knight" => [Find($"dragon-blade-{level}"), Find($"dragon-aegis-{level}")],
            "ranger" => [Find($"dragon-bow-{level}")],
            "rogue" => [Find($"dragon-fangs-{level}")],
            "mage" => [Find($"dragon-staff-{level}-{element.ToString().ToLowerInvariant()}")],
            _ => [Find($"dragon-shard-{level}")]
        };
    }

    public static ItemDefinition Get(string id) =>
        All.FirstOrDefault(item => item.Id == id)
        ?? Spares.FirstOrDefault(item => item.Id == id) as ItemDefinition
        ?? DragonShards.FirstOrDefault(item => item.Id == id) as ItemDefinition
        ?? throw new KeyNotFoundException($"No item with id '{id}' exists in the catalog.");

    private const string SparePrefix = "spare-";

    private const string DragonBound = "Forged from a shard of the Custode's heart. It grows with you and is bound to your account: it cannot be sold, thrown away or left in a grave.";

    private static string Tiered(string name, int tier) => tier switch { 2 => $"{name} II", 3 => $"{name} III", _ => name };

    private static string Earned(int tier) => $"Earned by defeating the Custode on {(Difficulty)(tier - 1)}.";

    private static WeaponDefinition DragonAxes(int tier) =>
        new($"dragon-shard-{tier}", Tiered("Asce del Drago", tier), Rarity.Platinum, WeaponType.Axe,
            [Attack(18 + 3 * tier), Defense(2 + tier), Critical(4 + tier)],
            $"Twin axes: every attack sweeps, then strikes the target again. {DragonBound} {Earned(tier)}")
        {
            IsTwin = true,
            IsBound = true,
            ScalesWithLevel = true
        };

    private static WeaponDefinition DragonBlade(int tier) =>
        new($"dragon-blade-{tier}", Tiered("Lama del Drago", tier), Rarity.Platinum, WeaponType.Sword,
            [Attack(16 + 3 * tier), Defense(1 + tier), Critical(4 + tier)],
            $"The knight's half of the pair, made to fight beside the Egida del Drago. {DragonBound} {Earned(tier)}")
        {
            IsBound = true,
            ScalesWithLevel = true
        };

    private static GearDefinition DragonAegis(int tier) =>
        new($"dragon-aegis-{tier}", Tiered("Egida del Drago", tier), Rarity.Platinum, EquipmentKind.Shield,
            [Defense(5 + 2 * tier), Block(24 + 3 * tier)],
            $"A shield of dragon scale that blocks far more often than any other. {DragonBound} {Earned(tier)}")
        {
            IsBound = true,
            ScalesWithLevel = true
        };

    private static WeaponDefinition DragonBow(int tier) =>
        new($"dragon-bow-{tier}", Tiered("Arco del Drago", tier), Rarity.Platinum, WeaponType.Bow,
            [Attack(17 + 3 * tier), Evasion(3 + tier), Critical(5 + tier)],
            $"Its arrows pierce every monster in line; each one after the first takes 75% damage. {DragonBound} {Earned(tier)}")
        {
            Pierces = true,
            IsBound = true,
            ScalesWithLevel = true
        };

    private static WeaponDefinition DragonFangs(int tier) =>
        new($"dragon-fangs-{tier}", Tiered("Zanne del Drago", tier), Rarity.Platinum, WeaponType.Dagger,
            [Attack(13 + 2 * tier), Critical(10 + 2 * tier), Evasion(3 + tier)],
            $"Twin daggers: every attack stabs twice at full strength. {DragonBound} {Earned(tier)}")
        {
            IsTwin = true,
            IsBound = true,
            ScalesWithLevel = true
        };

    private static WeaponDefinition DragonStaff(int tier, Element element)
    {
        var spell = SpellCatalog.Get((element, tier >= 3) switch
        {
            (Element.Fire, false) => "fireball",
            (Element.Fire, true) => "fire-rain",
            (Element.Ice, false) => "frost-nova",
            (Element.Ice, true) => "blizzard",
            (Element.Lightning, false) => "chain-lightning",
            (Element.Lightning, true) => "tempest",
            (Element.Wind, false) => "squall",
            _ => "tornado"
        });

        return new($"dragon-staff-{tier}-{element.ToString().ToLowerInvariant()}", Tiered("Bastone del Drago", tier), Rarity.Platinum, WeaponType.Staff,
            [Attack(12 + 3 * tier), Mana(10 + 5 * tier)],
            $"Casts {spell.Name} and holds a deep well of mana. {DragonBound} {Earned(tier)}",
            spell.Element)
        {
            Spell = spell,
            IsBound = true,
            ScalesWithLevel = true
        };
    }

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

    private static WeaponDefinition Exotic(string id, string name, WeaponType type, ExoticPower power, string description, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Exotic, type, modifiers, description) { Exotic = power };

    private static WeaponDefinition ExoticStaff(string id, string name, SpellDefinition spell, ExoticPower power, string description, params StatModifier[] modifiers) =>
        new(id, name, Rarity.Exotic, WeaponType.Staff, modifiers, description, spell.Element) { Spell = spell, Exotic = power };

    private static WeaponDefinition Staff(string id, string name, Rarity rarity, SpellDefinition spell, params StatModifier[] modifiers) =>
        new(id, name, rarity, WeaponType.Staff, modifiers, FixedElement: spell.Element) { Spell = spell };

    private static WeaponDefinition LegendaryStaff(
        string id,
        string name,
        SpellDefinition spell,
        string description,
        ItemEffect[] effects,
        params StatModifier[] modifiers) =>
        new(id, name, Rarity.Legendary, WeaponType.Staff, modifiers, description, spell.Element) { Spell = spell, Effects = effects };

    private static ScrollDefinition Scroll(string spellId, Rarity rarity, string description)
    {
        var spell = SpellCatalog.Get(spellId);
        return new ScrollDefinition("scroll-" + spellId, "Scroll of " + spell.Name, rarity, spell, description);
    }

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
