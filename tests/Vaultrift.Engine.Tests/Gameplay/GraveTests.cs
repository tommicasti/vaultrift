using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class GraveTests
{
    private const double NoCritical = 0.5;

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "#######",
        "#.....#",
        "#.....#",
        "#######"
    ]);

    private static Player NewFragilePlayer()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 2, attack: 5, defense: 0));
        player.Inventory.TryAdd(TestItems.Sword);
        player.Equip(player.Inventory.Entries[^1].Id);
        player.Inventory.TryAdd(TestItems.Armor);
        player.Equip(player.Inventory.Entries[^1].Id);
        return player;
    }

    private static readonly WeaponDefinition Spare = TestItems.Sword with { Id = "spare-test-sword", Name = "Spare Test Sword", IsSpare = true };

    private static readonly CharacterClass FragileKnight = new(
        "knight",
        "Knight",
        new Stats(maxHealth: 2, attack: 5, defense: 0),
        new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Defense, 1)))
    {
        SpareWeapon = Spare
    };

    private static Game NewKnightGame(Enemy enemy)
    {
        var player = new Player(new Position(1, 1), FragileKnight);
        player.Inventory.TryAdd(TestItems.Sword);
        player.Equip(player.Inventory.Entries[^1].Id);
        player.Inventory.TryAdd(TestItems.Armor);
        player.Equip(player.Inventory.Entries[^1].Id);
        return new Game(Map, player, [enemy], new FixedRandom(Enumerable.Repeat(NoCritical, 80).ToArray())) { Difficulty = Difficulty.Hard };
    }

    private static Enemy NewEnemy(int x = 3) => new(1, new Position(x, 1), new Stats(maxHealth: 10, attack: 4, defense: 1));

    private static Game NewGame(Difficulty difficulty, Enemy enemy) =>
        new(Map, NewFragilePlayer(), [enemy], new FixedRandom(Enumerable.Repeat(NoCritical, 50).ToArray())) { Difficulty = difficulty };

    [Fact]
    public void OnNormal_DyingKeepsTheEquipment()
    {
        var game = NewGame(Difficulty.Normal, NewEnemy());

        game.MovePlayer(Direction.Right);

        Assert.Equal(1, game.Deaths);
        Assert.Equal(2, game.Player.Equipment.Count);
        Assert.Null(game.Grave);
    }

    [Fact]
    public void OnHard_DyingLeavesTheEquipmentInAGrave()
    {
        var game = NewGame(Difficulty.Hard, NewEnemy());

        var result = game.MovePlayer(Direction.Right);

        Assert.Empty(game.Player.Equipment);
        Assert.NotNull(game.Grave);
        Assert.Equal(new Position(2, 1), game.Grave.Position);
        Assert.Equal(2, game.Grave.Count);
        Assert.Contains(new GraveDug(new Position(2, 1), 2), result.Events);
        Assert.Equal(new Position(1, 1), game.Player.Position);
    }

    [Fact]
    public void OnHard_TheHeroWakesUpWithTheSpareWeaponOfTheClass()
    {
        var game = NewKnightGame(NewEnemy());

        var result = game.MovePlayer(Direction.Right);

        Assert.Equal(Spare, game.Player.Weapon?.Definition);
        Assert.Single(game.Player.Equipment);
        Assert.Contains(game.Grave!.Items, item => item.Entry.Item.Definition == TestItems.Sword);
        Assert.Contains(result.Events, gameEvent => gameEvent is SpareWeaponTaken);
    }

    [Fact]
    public void WalkingOverTheGrave_SwapsTheSpareForTheRealWeapon()
    {
        var enemy = NewEnemy();
        var game = NewKnightGame(enemy);
        game.MovePlayer(Direction.Right);
        enemy.TakeDamage(enemy.Health);

        game.MovePlayer(Direction.Right);

        Assert.Null(game.Grave);
        Assert.Equal(TestItems.Sword, game.Player.Weapon?.Definition);
        Assert.Equal(2, game.Player.Equipment.Count);
        Assert.DoesNotContain(game.Player.Inventory.Entries, entry => entry.Item.Definition == Spare);
    }

    [Fact]
    public void DyingWithTheSpareInHand_DoesNotBuryIt()
    {
        var game = NewKnightGame(NewEnemy());
        game.MovePlayer(Direction.Right);
        var first = game.Grave!;

        var events = new List<GameEvent>();
        for (var turn = 0; turn < 5 && game.Deaths < 2; turn++)
            events.AddRange(game.Attack().Events);

        Assert.Equal(2, game.Deaths);
        Assert.Contains(new GraveLost(first.Position, 2), events);
        Assert.Null(game.Grave);
        Assert.Equal(Spare, game.Player.Weapon?.Definition);
    }

    [Fact]
    public void TheSpareWeapon_IsWorthNothing()
    {
        Assert.Equal(0, ItemPrices.SellValue(Spare, 10));
    }

    [Fact]
    public void OnHard_DyingKeepsTheCoins()
    {
        var game = NewGame(Difficulty.Hard, NewEnemy());
        game.Player.GainCoins(100);

        var result = game.MovePlayer(Direction.Right);

        Assert.Equal(100, game.Player.Coins);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is CoinsLost);
    }

    [Fact]
    public void OnNormal_DyingCostsCoins()
    {
        var game = NewGame(Difficulty.Normal, NewEnemy());
        game.Player.GainCoins(100);

        game.MovePlayer(Direction.Right);

        Assert.True(game.Player.Coins < 100);
    }

    [Fact]
    public void WalkingOverTheGrave_PutsEverythingBackOn()
    {
        var enemy = NewEnemy();
        var game = NewGame(Difficulty.Hard, enemy);
        game.MovePlayer(Direction.Right);
        enemy.TakeDamage(enemy.Health);

        var result = game.MovePlayer(Direction.Right);

        Assert.Null(game.Grave);
        Assert.Equal(2, game.Player.Equipment.Count);
        Assert.Contains(new GraveRecovered(2, 0), result.Events);
    }

    [Fact]
    public void DyingAgainBeforeRecovering_LosesTheFirstGrave()
    {
        var game = NewGame(Difficulty.Hard, NewEnemy());
        game.MovePlayer(Direction.Right);
        var first = game.Grave!;
        game.Player.Inventory.TryAdd(TestItems.Ring);
        game.Player.Equip(game.Player.Inventory.Entries[^1].Id);

        var events = new List<GameEvent>();
        for (var turn = 0; turn < 5 && game.Deaths < 2; turn++)
            events.AddRange(game.Attack().Events);

        Assert.Equal(2, game.Deaths);
        Assert.Contains(new GraveLost(first.Position, 2), events);
        Assert.Equal(1, game.Grave!.Count);
    }

    [Fact]
    public void ASlotTakenInTheMeantime_SendsTheGraveItemToTheBackpack()
    {
        var enemy = NewEnemy();
        var game = NewGame(Difficulty.Hard, enemy);
        game.MovePlayer(Direction.Right);
        enemy.TakeDamage(enemy.Health);
        game.Player.Inventory.TryAdd(TestItems.Armor);
        var replacement = game.Player.Inventory.Entries[^1].Id;
        game.Player.Equip(replacement);

        game.MovePlayer(Direction.Right);

        Assert.Equal(replacement, game.Player.Equipment[EquipmentSlot.Body].Id);
        Assert.Contains(game.Player.Inventory.Entries, entry => entry.Item.Definition == TestItems.Armor);
    }

    [Fact]
    public void OnHardcore_DeathEndsTheGame()
    {
        var game = NewGame(Difficulty.Hardcore, NewEnemy());

        var result = game.MovePlayer(Direction.Right);

        Assert.True(game.IsLost);
        Assert.Equal(1, game.Deaths);
        Assert.Contains(new HeroLost(), result.Events);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is PlayerRespawned);
    }

    [Fact]
    public void AfterAHardcoreDeath_NothingElseHappens()
    {
        var enemy = NewEnemy();
        var game = NewGame(Difficulty.Hardcore, enemy);
        game.MovePlayer(Direction.Right);
        var turn = game.TurnNumber;

        var moved = game.MovePlayer(Direction.Down);
        var attacked = game.Attack();

        Assert.False(moved.TurnPassed);
        Assert.False(attacked.TurnPassed);
        Assert.Equal(turn, game.TurnNumber);
        Assert.Equal(enemy.Position, enemy.Position);
    }
}
