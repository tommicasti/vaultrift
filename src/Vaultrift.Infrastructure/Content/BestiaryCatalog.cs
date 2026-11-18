using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Content;

public static class BestiaryCatalog
{
    public static IReadOnlyList<EnemyKind> All { get; } =
    [
        new("rat", "Rat", new Stats(maxHealth: 6, attack: 3, defense: 0, evasion: 5),
            weakness: Element.Fire),
        new("bat", "Bat", new Stats(maxHealth: 5, attack: 3, defense: 0, evasion: 20), Speed.Fast,
            weakness: Element.Lightning, resistance: Element.Wind, attackElement: Element.Wind),
        new("spider", "Spider", new Stats(maxHealth: 9, attack: 4, defense: 1, evasion: 5),
            weakness: Element.Fire),
        new("slime", "Slime", new Stats(maxHealth: 16, attack: 4, defense: 1), Speed.Slow,
            weakness: Element.Lightning, resistance: Element.Ice),
        new("ghost", "Ghost", new Stats(maxHealth: 7, attack: 4, defense: 0, evasion: 15), passesThroughWalls: true,
            weakness: Element.Wind, resistance: Element.Ice, attackElement: Element.Ice),
        new("cyclops", "Cyclops", new Stats(maxHealth: 22, attack: 7, defense: 3),
            weakness: Element.Ice, resistance: Element.Fire, attackElement: Element.Fire),
        new("mimic", "Mimic", new Stats(maxHealth: 26, attack: 8, defense: 3), isStationary: true),

        new("rat-king", "Rat King", new Stats(maxHealth: 45, attack: 6, defense: 1, evasion: 10),
            weakness: Element.Fire, isBoss: true),
        new("spider-queen", "Spider Queen", new Stats(maxHealth: 45, attack: 6, defense: 1, evasion: 10), Speed.Fast,
            weakness: Element.Fire, resistance: Element.Wind, isBoss: true),
        new("troll", "Troll", new Stats(maxHealth: 70, attack: 9, defense: 3), Speed.Slow,
            weakness: Element.Lightning, resistance: Element.Ice, isBoss: true),
        new("dune-scorpion", "Dune Scorpion", new Stats(maxHealth: 50, attack: 7, defense: 3, evasion: 5),
            weakness: Element.Ice, resistance: Element.Fire, attackElement: Element.Fire, isBoss: true),
        new("frost-yeti", "Frost Yeti", new Stats(maxHealth: 60, attack: 7, defense: 2),
            weakness: Element.Fire, resistance: Element.Ice, attackElement: Element.Ice, isBoss: true),
        new("black-knight", "Black Knight", new Stats(maxHealth: 55, attack: 7, defense: 5),
            weakness: Element.Lightning, resistance: Element.Wind, isBoss: true)
    ];

    public static IReadOnlyList<EnemyKind> Monsters { get; } = All.Where(kind => !kind.IsBoss).ToList();

    public static EnemyKind Get(string id) =>
        All.FirstOrDefault(kind => kind.Id == id)
        ?? throw new KeyNotFoundException($"Unknown enemy kind '{id}'.");
}
