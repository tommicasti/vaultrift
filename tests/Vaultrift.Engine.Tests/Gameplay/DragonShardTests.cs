using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class DragonShardTests
{
    private const double NoCritical = 0.5;

    private static readonly WeaponDefinition Shard =
        new("test-shard", "Test Shard", Rarity.Platinum, WeaponType.Axe, [new StatModifier(StatType.Attack, 10)])
        {
            IsTwin = true,
            IsBound = true,
            ScalesWithLevel = true
        };

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#.....#",
        "#######"
    ]);

    private static Player PlayerWithShard(int maxHealth = 40)
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: maxHealth, attack: 5, defense: 0)) { Facing = Direction.Right };
        Assert.True(player.Inventory.TryAddBound(new ItemInstance(Shard)));
        player.Equip(player.Inventory.Entries[^1].Id);
        return player;
    }

    private static Game NewGame(Player player, Enemy enemy, Difficulty difficulty = Difficulty.Normal) =>
        new(Map, player, [enemy], new FixedRandom(Enumerable.Repeat(NoCritical, 80).ToArray())) { Difficulty = difficulty };

    [Fact]
    public void TwinAxes_SweepAndThenStrikeASecondTime()
    {
        var enemy = new Enemy(1, new Position(2, 1), new Stats(maxHealth: 200, attack: 1, defense: 0));
        var game = NewGame(PlayerWithShard(), enemy);

        var result = game.Attack();

        Assert.Equal(2, result.Events.OfType<PlayerAttackedEnemy>().Count(hit => hit.EnemyId == 1));
    }

    [Fact]
    public void ABoundWeapon_IsAFavoriteThatCannotBeDiscardedOrMarkedAsJunk()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 40, attack: 5, defense: 0));
        player.Inventory.TryAddBound(new ItemInstance(Shard));
        var entry = player.Inventory.Entries[^1];
        var game = NewGame(player, new Enemy(1, new Position(5, 2), new Stats(maxHealth: 10, attack: 1, defense: 0)));

        Assert.Equal(ItemMark.Favorite, entry.Mark);
        Assert.Contains(new UseRejected(UseResult.ItemIsFavorite), game.Mark(entry.Id, ItemMark.Junk).Events);
        Assert.Contains(new UseRejected(UseResult.ItemIsFavorite), game.Discard(entry.Id).Events);
        Assert.Equal(ItemMark.Favorite, entry.Mark);
    }

    [Fact]
    public void OnHard_ABoundWeaponStaysInYourHandsWhenYouDie()
    {
        var player = PlayerWithShard(maxHealth: 2);
        player.Inventory.TryAdd(TestItems.Armor);
        player.Equip(player.Inventory.Entries[^1].Id);
        var game = NewGame(player, new Enemy(1, new Position(3, 1), new Stats(maxHealth: 500, attack: 4, defense: 1)), Difficulty.Hard);

        game.MovePlayer(Direction.Right);

        Assert.Equal(1, game.Deaths);
        Assert.Equal(Shard, game.Player.Weapon?.Definition);
        Assert.DoesNotContain(game.Grave!.Items, item => item.Entry.Item.Definition == Shard);
    }

    [Fact]
    public void TheShard_GrowsWithTheLevel()
    {
        Assert.Equal(0, Player.LevelAttackBonus(1));
        Assert.Equal(10, Player.LevelAttackBonus(20));
        Assert.Equal(4, Player.LevelCriticalBonus(20));
        Assert.Equal(15, PlayerWithShard().CombatStats.Attack);
    }
}
