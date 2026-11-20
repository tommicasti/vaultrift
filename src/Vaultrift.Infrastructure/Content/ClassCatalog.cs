using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Content;

public static class ClassCatalog
{
    public static IReadOnlyList<ClassDefinition> All { get; } =
    [
        new(
            new CharacterClass(
                "knight",
                "Knight",
                new Stats(maxHealth: 32, attack: 2, defense: 3, evasion: 3, maxMana: 10),
                new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Defense, 2)),
                Ability: ClassAbility.Bulwark)
            {
                SpareWeapon = ItemCatalog.Spare("sword-common")
            },
            "A steady defender with sword and shield. Bulwark halves every blow for a few turns, and stops them cold with a shield.",
            ["sword-common", "shield-buckler", "potion-healing", "potion-healing"]),
        new(
            new CharacterClass(
                "berserker",
                "Berserker",
                new Stats(maxHealth: 34, attack: 4, defense: 1, evasion: 5, maxMana: 10),
                new WeaponMastery(WeaponType.Axe, new StatModifier(StatType.Attack, 2)),
                Ability: ClassAbility.Fury)
            {
                SpareWeapon = ItemCatalog.Spare("axe-common")
            },
            "Hits harder than anyone, especially with an axe. Fury raises attack by half for five turns.",
            ["axe-common", "potion-healing", "potion-healing"]),
        new(
            new CharacterClass(
                "ranger",
                "Ranger",
                new Stats(maxHealth: 26, attack: 3, defense: 1, evasion: 12, maxMana: 15),
                new WeaponMastery(WeaponType.Bow, new StatModifier(StatType.Attack, 2)),
                Ability: ClassAbility.PowerShot)
            {
                SpareWeapon = ItemCatalog.Spare("bow-common")
            },
            "Light on their feet and deadly with a bow. Power Shot pierces every enemy in a line for double damage.",
            ["bow-common", "potion-healing", "potion-healing", "potion-healing"]),
        new(
            new CharacterClass(
                "rogue",
                "Rogue",
                new Stats(maxHealth: 24, attack: 3, defense: 1, evasion: 15, critical: 20, maxMana: 15),
                new WeaponMastery(WeaponType.Dagger, new StatModifier(StatType.Critical, 10)),
                Speed.Fast,
                ClassAbility.Invisibility)
            {
                SpareWeapon = ItemCatalog.Spare("dagger-common")
            },
            "Two steps ahead of everyone. Invisibility hides you for five turns; the first strike from the shadows is always critical.",
            ["dagger-common", "potion-healing", "potion-healing", "potion-healing"]),
        new(
            new CharacterClass(
                "mage",
                "Mage",
                new Stats(maxHealth: 22, attack: 3, defense: 0, evasion: 5, maxMana: 30),
                new WeaponMastery(WeaponType.Staff, new StatModifier(StatType.Attack, 2)),
                Ability: ClassAbility.Overload)
            {
                SpareWeapon = ItemCatalog.Spare("staff-oak")
            },
            "Fragile but devastating from afar. Spells cost one less mana with a staff. Overload makes the next spell free and wider.",
            ["staff-oak", "potion-healing", "potion-healing", "potion-mana"])
    ];

    public static ClassDefinition? Find(string id) =>
        All.FirstOrDefault(definition => definition.Class.Id == id);
}