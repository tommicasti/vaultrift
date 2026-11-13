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
                new Stats(maxHealth: 32, attack: 2, defense: 3, evasion: 3),
                new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Defense, 2))),
            "A steady defender. Parries better with a sword in hand.",
            ["sword-common", "potion-healing", "potion-healing"]),
        new(
            new CharacterClass(
                "berserker",
                "Berserker",
                new Stats(maxHealth: 34, attack: 4, defense: 1, evasion: 5),
                new WeaponMastery(WeaponType.Axe, new StatModifier(StatType.Attack, 2))),
            "Hits harder than anyone, especially with an axe. Defense is an afterthought.",
            ["axe-common", "potion-healing", "potion-healing"]),
        new(
            new CharacterClass(
                "ranger",
                "Ranger",
                new Stats(maxHealth: 26, attack: 3, defense: 1, evasion: 12),
                new WeaponMastery(WeaponType.Bow, new StatModifier(StatType.Attack, 2))),
            "Light on their feet and deadly with a bow.",
            ["bow-common", "potion-healing", "potion-healing"])
    ];

    public static ClassDefinition? Find(string id) =>
        All.FirstOrDefault(definition => definition.Class.Id == id);
}