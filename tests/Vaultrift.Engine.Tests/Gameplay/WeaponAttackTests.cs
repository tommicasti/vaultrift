using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class WeaponAttackTests
{
    private const double NoCritical = 0.99;

    private static readonly DungeonMap OpenRoom = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#........#",
        "#........#",
        "##########"
    ]);

    private static readonly DungeonMap SplitRoom = DungeonMap.Parse(
    [
        "##########",
        "#...#....#",
        "##########"
    ]);

    private static readonly WeaponDefinition Bow =
        new("test-bow", "Test Bow", Rarity.Common, WeaponType.Bow, [new StatModifier(StatType.Attack, 3)]);

    private static readonly WeaponDefinition Axe =
        new("test-axe", "Test Axe", Rarity.Common, WeaponType.Axe, [new StatModifier(StatType.Attack, 3)]);

    private static readonly WeaponDefinition Sword =
        new("test-sword", "Test Sword", Rarity.Common, WeaponType.Sword, [new StatModifier(StatType.Attack, 3)]);

    private static Game NewGame(DungeonMap map, WeaponDefinition weapon, Position start, Direction facing, params Enemy[] enemies)
    {
        var player = new Player(start, new Stats(maxHealth: 30, attack: 5, defense: 2)) { Facing = facing };
        player.Inventory.TryAdd(new ItemInstance(weapon));
        player.Equip(player.Inventory.Entries[0].Id);

        return new Game(map, player, enemies, new FixedRandom(Enumerable.Repeat(NoCritical, 20).ToArray()));
    }

    private static Enemy NewEnemy(int id, int x, int y) =>
        new(id, new Position(x, y), new Stats(maxHealth: 50, attack: 1, defense: 0));

    private static int DamageTo(TurnResult result, int enemyId) =>
        result.Events.OfType<PlayerAttackedEnemy>().Single(hit => hit.EnemyId == enemyId).Result.Damage;

    [Fact]
    public void Bow_HitsAnEnemyFarDownTheLine()
    {
        var game = NewGame(OpenRoom, Bow, new Position(1, 2), Direction.Right, NewEnemy(1, 6, 2));

        var result = game.Attack();

        Assert.Equal(8, DamageTo(result, 1));
        Assert.Equal(5, result.Events.OfType<ArrowFlew>().Single().Path.Count);
    }

    [Fact]
    public void Bow_StopsAtTheFirstEnemy()
    {
        var game = NewGame(OpenRoom, Bow, new Position(1, 2), Direction.Right, NewEnemy(1, 3, 2), NewEnemy(2, 5, 2));

        var result = game.Attack();

        Assert.Single(result.Events.OfType<PlayerAttackedEnemy>(), hit => hit.EnemyId == 1);
        Assert.DoesNotContain(result.Events.OfType<PlayerAttackedEnemy>(), hit => hit.EnemyId == 2);
    }

    [Fact]
    public void Bow_IsStoppedByWalls()
    {
        var game = NewGame(SplitRoom, Bow, new Position(1, 1), Direction.Right, NewEnemy(1, 6, 1));

        var result = game.Attack();

        Assert.Contains(new PlayerAttackedNothing(), result.Events);
        Assert.Equal(new[] { new Position(2, 1), new Position(3, 1) }, result.Events.OfType<ArrowFlew>().Single().Path);
    }

    [Fact]
    public void Bow_CannotReachBeyondItsRange()
    {
        var game = NewGame(OpenRoom, Bow, new Position(1, 2), Direction.Right, NewEnemy(1, 1 + Game.BowRange + 1, 2));

        var result = game.Attack();

        Assert.Contains(new PlayerAttackedNothing(), result.Events);
        Assert.Equal(Game.BowRange, result.Events.OfType<ArrowFlew>().Single().Path.Count);
    }

    [Fact]
    public void Bow_AtPointBlank_DealsHalfDamage()
    {
        var game = NewGame(OpenRoom, Bow, new Position(1, 2), Direction.Right, NewEnemy(1, 2, 2));

        Assert.Equal(4, DamageTo(game.Attack(), 1));
    }

    [Fact]
    public void Bow_WakesUpATargetBeyondTheWakeDistance()
    {
        var enemy = NewEnemy(1, 7, 2);
        var game = NewGame(OpenRoom, Bow, new Position(1, 2), Direction.Right, enemy);

        var result = game.Attack();

        Assert.True(enemy.IsAwake);
        Assert.Single(result.Events.OfType<EnemyWokeUp>());
    }

    [Fact]
    public void Axe_HitsTheTargetFully_AndBothSidesForHalf()
    {
        var game = NewGame(OpenRoom, Axe, new Position(2, 2), Direction.Right,
            NewEnemy(1, 3, 2), NewEnemy(2, 3, 1), NewEnemy(3, 3, 3));

        var result = game.Attack();

        Assert.Equal(8, DamageTo(result, 1));
        Assert.Equal(4, DamageTo(result, 2));
        Assert.Equal(4, DamageTo(result, 3));
        Assert.Equal(new[] { new Position(3, 2), new Position(3, 1), new Position(3, 3) }, result.Events.OfType<PlayerSwept>().Single().Tiles);
    }

    [Fact]
    public void Axe_WithOnlyASideEnemy_StillHitsIt()
    {
        var game = NewGame(OpenRoom, Axe, new Position(2, 2), Direction.Right, NewEnemy(1, 3, 1));

        Assert.Equal(4, DamageTo(game.Attack(), 1));
    }

    [Fact]
    public void Axe_WithNobodyAround_AttacksNothing()
    {
        var game = NewGame(OpenRoom, Axe, new Position(2, 2), Direction.Right, NewEnemy(1, 7, 3));

        Assert.Contains(new PlayerAttackedNothing(), game.Attack().Events);
    }

    [Fact]
    public void Sword_OnlyHitsTheTileInFront()
    {
        var game = NewGame(OpenRoom, Sword, new Position(2, 2), Direction.Right, NewEnemy(1, 3, 1), NewEnemy(2, 4, 2));

        var result = game.Attack();

        Assert.Contains(new PlayerAttackedNothing(), result.Events);
        Assert.Empty(result.Events.OfType<PlayerAttackedEnemy>());
    }
}