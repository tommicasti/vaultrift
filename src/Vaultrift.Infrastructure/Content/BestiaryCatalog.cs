using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Infrastructure.Content;

public static class BestiaryCatalog
{
    public static IReadOnlyList<EnemyKind> All { get; } =
    [
        new("rat", "Rat", new Stats(maxHealth: 6, attack: 3, defense: 0, evasion: 5)),
        new("bat", "Bat", new Stats(maxHealth: 5, attack: 3, defense: 0, evasion: 20), Speed.Fast),
        new("spider", "Spider", new Stats(maxHealth: 9, attack: 4, defense: 1, evasion: 5)),
        new("slime", "Slime", new Stats(maxHealth: 16, attack: 4, defense: 1), Speed.Slow),
        new("ghost", "Ghost", new Stats(maxHealth: 7, attack: 4, defense: 0, evasion: 15), passesThroughWalls: true),
        new("cyclops", "Cyclops", new Stats(maxHealth: 22, attack: 7, defense: 3))
    ];

    public static EnemyKind Get(string id) =>
        All.FirstOrDefault(kind => kind.Id == id)
        ?? throw new KeyNotFoundException($"Unknown enemy kind '{id}'.");
}