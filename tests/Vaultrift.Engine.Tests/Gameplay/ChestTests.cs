using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class ChestTests
{
    private static readonly DungeonMap Hall = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#........#",
        "##########"
    ]);

    private static readonly ItemInstance Treasure =
        new(new WeaponDefinition("test-treasure", "Treasure", Rarity.Epic, WeaponType.Sword, [new StatModifier(StatType.Attack, 5)]));

    private static readonly EnemyKind MimicKind =
        new("test-mimic", "Mimic", new Stats(maxHealth: 30, attack: 6, defense: 0), isStationary: true);

    private static Player NewPlayer() =>
        new(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 0)) { Facing = Direction.Right };

    private static Game NewGame(Player player, IEnumerable<Chest> chests, ILootTable? loot = null, params Enemy[] enemies) =>
        new(player, new SingleFloor(new Floor(Hall, player.Position, enemies, [], Chests: chests.ToList())),
            new FixedRandom(Dice.AlwaysFail()), loot);

    [Fact]
    public void AttackingAChest_OpensIt_AndLeavesTheLootOnItsTile()
    {
        var game = NewGame(NewPlayer(), [new Chest(new Position(2, 1))], new FixedLoot(Treasure));

        var result = game.Attack();

        Assert.Contains(new ChestOpened(new Position(2, 1), Treasure), result.Events);
        Assert.Empty(game.Chests);
        Assert.Same(Treasure, game.FloorItems[new Position(2, 1)]);
    }

    [Fact]
    public void AChest_WithoutALootTable_IsEmpty()
    {
        var game = NewGame(NewPlayer(), [new Chest(new Position(2, 1))]);

        Assert.Contains(new ChestOpened(new Position(2, 1), null), game.Attack().Events);
        Assert.Empty(game.FloorItems);
    }

    [Fact]
    public void OpeningAChest_IsNotAnAttack()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);
        player.Give(TestGear.Dagger);
        var game = NewGame(player, [new Chest(new Position(2, 1))], new FixedLoot(Treasure));

        var result = game.Attack();

        Assert.True(result.TurnPassed);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is PlayerAttackedEnemy or PlayerAttackedNothing);
    }

    [Fact]
    public void AChest_BlocksTheWay()
    {
        var player = NewPlayer();
        var game = NewGame(player, [new Chest(new Position(2, 1))]);

        var result = game.MovePlayer(Direction.Right);

        Assert.False(result.TurnPassed);
        Assert.Equal(new Position(1, 1), player.Position);
    }

    [Fact]
    public void AChest_StopsArrows()
    {
        var player = NewPlayer();
        player.Give(TestGear.Bow);
        var enemy = new Enemy(1, new Position(5, 1), new Stats(maxHealth: 20, attack: 1, defense: 0));
        var game = NewGame(player, [new Chest(new Position(3, 1))], enemies: enemy);

        var result = game.Attack();

        Assert.Equal(new[] { new Position(2, 1) }, result.Events.OfType<ArrowFlew>().Single().Path);
        Assert.Equal(20, enemy.Health);
    }

    [Fact]
    public void AMimic_RevealsItself_AndBitesAtOnce()
    {
        var mimic = new Enemy(7, new Position(2, 1), MimicKind);
        var game = NewGame(NewPlayer(), [new Chest(new Position(2, 1), mimic)], new FixedLoot(Treasure));

        var result = game.Attack();

        Assert.Contains(new MimicRevealed(7, new Position(2, 1)), result.Events);
        Assert.Contains(result.Events, gameEvent => gameEvent is EnemyAttackedPlayer { EnemyId: 7 });
        Assert.Contains(mimic, game.Enemies);
        Assert.Empty(game.FloorItems);
    }

    [Fact]
    public void AMimic_NeverLeavesItsSpot()
    {
        var mimic = new Enemy(7, new Position(2, 1), MimicKind);
        var player = NewPlayer();
        var game = NewGame(player, [new Chest(new Position(2, 1), mimic)]);
        game.Attack();

        game.MovePlayer(Direction.Down);
        game.MovePlayer(Direction.Left);

        Assert.Equal(new Position(2, 1), mimic.Position);
    }

    [Fact]
    public void AChest_OnAWall_IsRejected()
    {
        var player = NewPlayer();

        Assert.Throws<ArgumentException>(() =>
            new Game(player, new SingleFloor(new Floor(Hall, player.Position, [], [], Chests: [new Chest(new Position(0, 0))])), new FixedRandom()));
    }

    [Fact]
    public void AMimic_OutsideItsChest_IsRejected()
    {
        var player = NewPlayer();
        var mimic = new Enemy(7, new Position(3, 1), MimicKind);

        Assert.Throws<ArgumentException>(() =>
            new Game(player, new SingleFloor(new Floor(Hall, player.Position, [], [], Chests: [new Chest(new Position(2, 1), mimic)])), new FixedRandom()));
    }

    [Fact]
    public void Discarding_RemovesTheWholeStack_WithoutPassingTheTurn()
    {
        var player = NewPlayer();
        player.Inventory.TryAdd(TestItems.HealingPotion);
        player.Inventory.TryAdd(TestItems.HealingPotion);
        var game = NewGame(player, []);

        var result = game.Discard(player.Inventory.Entries[0].Id);

        Assert.False(result.TurnPassed);
        Assert.True(Assert.Single(result.Events) is ItemDiscarded);
        Assert.Empty(player.Inventory.Entries);
    }

    [Fact]
    public void Discarding_AnUnknownEntry_IsRejected()
    {
        var game = NewGame(NewPlayer(), []);

        Assert.Contains(new UseRejected(UseResult.ItemNotFound), game.Discard(99).Events);
    }
}
