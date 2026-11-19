using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Content;

public static class BestiaryCatalog
{
    private static readonly EnemyKind Sporeling =
        new("sporeling", "Sporeling", new Stats(maxHealth: 6, attack: 4, defense: 0),
            weakness: Element.Fire);

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
        new("salamander", "Salamander", new Stats(maxHealth: 10, attack: 5, defense: 1, evasion: 10),
            weakness: Element.Ice, resistance: Element.Fire, attackElement: Element.Fire),
        new("fire-imp", "Fire Imp", new Stats(maxHealth: 7, attack: 4, defense: 0, evasion: 20), Speed.Fast,
            weakness: Element.Ice, resistance: Element.Fire, attackElement: Element.Fire),
        new("crab", "Crab", new Stats(maxHealth: 16, attack: 5, defense: 4), Speed.Slow,
            weakness: Element.Lightning, resistance: Element.Fire),
        new("eel", "Eel", new Stats(maxHealth: 9, attack: 5, defense: 0, evasion: 15), Speed.Fast,
            weakness: Element.Ice, resistance: Element.Lightning, attackElement: Element.Lightning),
        new("gem-beetle", "Gem Beetle", new Stats(maxHealth: 18, attack: 5, defense: 5), Speed.Slow,
            weakness: Element.Lightning, resistance: Element.Ice),
        new("shardling", "Shardling", new Stats(maxHealth: 8, attack: 5, defense: 1, evasion: 15), Speed.Fast,
            weakness: Element.Wind, resistance: Element.Ice, attackElement: Element.Ice),
        new("myconid", "Myconid", new Stats(maxHealth: 14, attack: 5, defense: 2),
            weakness: Element.Fire, resistance: Element.Lightning),
        new("puffball", "Puffball", new Stats(maxHealth: 6, attack: 4, defense: 0, evasion: 15), Speed.Fast,
            weakness: Element.Fire, resistance: Element.Wind),
        Sporeling,
        new("harpy", "Harpy", new Stats(maxHealth: 9, attack: 5, defense: 0, evasion: 20), Speed.Fast,
            weakness: Element.Lightning, resistance: Element.Wind, attackElement: Element.Wind),
        new("gargoyle", "Gargoyle", new Stats(maxHealth: 24, attack: 6, defense: 5), Speed.Slow,
            weakness: Element.Ice, resistance: Element.Fire),

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
            weakness: Element.Lightning, resistance: Element.Wind, isBoss: true),
        new("magma-golem", "Magma Golem", new Stats(maxHealth: 70, attack: 8, defense: 4), Speed.Slow,
            weakness: Element.Ice, resistance: Element.Fire, attackElement: Element.Fire, isBoss: true, power: BossPower.LavaRing),
        new("kraken", "Kraken", new Stats(maxHealth: 60, attack: 7, defense: 2),
            weakness: Element.Lightning, resistance: Element.Ice, attackElement: Element.Ice, isBoss: true, power: BossPower.TentaclePull),
        new("crystal-golem", "Crystal Golem", new Stats(maxHealth: 70, attack: 7, defense: 5), Speed.Slow,
            weakness: Element.Lightning, resistance: Element.Ice, isBoss: true, power: BossPower.CrystalBloom),
        new("mushroom-king", "Mushroom King", new Stats(maxHealth: 60, attack: 7, defense: 2),
            weakness: Element.Fire, resistance: Element.Lightning, isBoss: true, power: BossPower.SummonMinions, minion: Sporeling),
        new("storm-griffin", "Storm Griffin", new Stats(maxHealth: 55, attack: 7, defense: 3, evasion: 10),
            weakness: Element.Ice, resistance: Element.Wind, attackElement: Element.Lightning, isBoss: true, power: BossPower.Gust)
    ];

    public static IReadOnlyList<EnemyKind> Monsters { get; } = All.Where(kind => !kind.IsBoss).ToList();

    public static EnemyKind Get(string id) =>
        All.FirstOrDefault(kind => kind.Id == id)
        ?? throw new KeyNotFoundException($"Unknown enemy kind '{id}'.");
}
