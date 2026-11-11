using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;

namespace Vaultrift.Infrastructure.Levels;

public static class StarterLevel
{
    private static readonly string[] Rows =
    [
        "####################",
        "#......#...........#",
        "#......#....####...#",
        "#..........#.......#",
        "#......#...#...#...#",
        "####.###...#...#...#",
        "#..........#...#...#",
        "#..####....#.......#",
        "#..........####....#",
        "####################"
    ];

    private static readonly Stats PlayerStats = new(maxHealth: 30, attack: 5, defense: 2);
    private static readonly Stats WeakEnemy = new(maxHealth: 8, attack: 3, defense: 0);
    private static readonly Stats StrongEnemy = new(maxHealth: 12, attack: 5, defense: 1);

    public static Game CreateGame(IRandom random)
    {
        var map = DungeonMap.Parse(Rows);
        var player = new Player(new Position(1, 1), PlayerStats);

        Enemy[] enemies =
        [
            new(1, new Position(5, 3), WeakEnemy),
            new(2, new Position(14, 1), WeakEnemy),
            new(3, new Position(2, 6), StrongEnemy),
            new(4, new Position(17, 7), StrongEnemy),
            new(5, new Position(9, 8), WeakEnemy)
        ];

        return new Game(map, player, enemies, random);
    }
}