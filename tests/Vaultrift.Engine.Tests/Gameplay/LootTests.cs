using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class LootTests
{
    private const double NoCritical = 0.99;

    private static readonly DungeonMap Corridor = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#.....#",
        "#######"
    ]);

    private static readonly DungeonMap SplitHall = DungeonMap.Parse(
    [
        "#######",
        "#..#..#",
        "#######"
    ]);

    private static readonly ItemInstance Sword =
        new(new WeaponDefinition("test-sword", "Test Sword", Rarity.Epic, WeaponType.Sword, [new StatModifier(StatType.Attack, 3)]));

    private static readonly ItemInstance Potion = new(new PotionDefinition("test-potion", "Test Potion", Rarity.Common, Healing: 5));

    private static readonly EnemyKind Rat = new("test-rat", "Rat", new Stats(maxHealth: 6, attack: 1, defense: 0));

    private static readonly EnemyKind Ghost =
        new("test-ghost", "Ghost", new Stats(maxHealth: 6, attack: 1, defense: 0), passesThroughWalls: true);

    private static Player NewPlayer(Position at) =>
        new(at, new Stats(maxHealth: 30, attack: 100, defense: 0)) { Facing = Direction.Right };

    private static Game NewGame(DungeonMap map, Player player, Enemy enemy, ILootTable? loot, IEnumerable<FloorItem>? items = null) =>
        new(player, SingleFloor.From(map, player, [enemy], items), new FixedRandom(NoCritical, NoCritical), loot);

    [Fact]
    public void ADefeatedEnemy_DropsItsLootOnItsTile()
    {
        var player = NewPlayer(new Position(1, 1));
        var game = NewGame(Corridor, player, new Enemy(1, new Position(2, 1), Rat), new FixedLoot(Sword));

        var result = game.Attack();

        Assert.Contains(new ItemDropped(1, Sword, new Position(2, 1)), result.Events);
        Assert.Same(Sword, game.FloorItems[new Position(2, 1)]);
    }

    [Fact]
    public void TheDrop_ComesAfterTheExperience()
    {
        var player = NewPlayer(new Position(1, 1));
        var game = NewGame(Corridor, player, new Enemy(1, new Position(2, 1), Rat), new FixedLoot(Sword));

        var events = game.Attack().Events.ToList();

        Assert.True(events.FindIndex(e => e is ExperienceGained) < events.FindIndex(e => e is ItemDropped));
    }

    [Fact]
    public void AnEnemyThatDropsNothing_LeavesTheFloorEmpty()
    {
        var player = NewPlayer(new Position(1, 1));
        var game = NewGame(Corridor, player, new Enemy(1, new Position(2, 1), Rat), new FixedLoot(null));

        var result = game.Attack();

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is ItemDropped);
        Assert.Empty(game.FloorItems);
    }

    [Fact]
    public void WithoutALootTable_NothingIsEverDropped()
    {
        var player = NewPlayer(new Position(1, 1));
        var game = NewGame(Corridor, player, new Enemy(1, new Position(2, 1), Rat), loot: null);

        game.Attack();

        Assert.Empty(game.FloorItems);
    }

    [Fact]
    public void WhenTheTileIsTaken_TheDropLandsNextToIt()
    {
        var player = NewPlayer(new Position(1, 1));
        var items = new[] { new FloorItem(new Position(2, 1), Potion) };
        var game = NewGame(Corridor, player, new Enemy(1, new Position(2, 1), Rat), new FixedLoot(Sword), items);

        var result = game.Attack();

        var dropped = Assert.Single(result.Events.OfType<ItemDropped>());
        Assert.NotEqual(new Position(2, 1), dropped.At);
        Assert.NotEqual(player.Position, dropped.At);
        Assert.Same(Potion, game.FloorItems[new Position(2, 1)]);
        Assert.Same(Sword, game.FloorItems[dropped.At]);
    }

    [Fact]
    public void AGhostDefeatedInsideAWall_DropsOnTheFloorBeyondIt()
    {
        var player = NewPlayer(new Position(2, 1));
        var ghost = new Enemy(1, new Position(4, 1), Ghost);
        var game = NewGame(SplitHall, player, ghost, new FixedLoot(Sword));
        ghost.Position = new Position(3, 1);

        var result = game.Attack();

        Assert.Contains(new ItemDropped(1, Sword, new Position(4, 1)), result.Events);
    }
}