using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Persistence;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class RiftTests
{
    private static readonly EnemyKind BossKind = new("test-boss", "Test Boss", new Stats(maxHealth: 3, attack: 1, defense: 0), isBoss: true);

    private static readonly Position Start = new(1, 1);
    private static readonly Position Stone = new(1, 2);
    private static readonly Position StairsAt = new(5, 1);
    private static readonly Position BossAt = new(3, 1);

    private static Floor NewFloor(int number) => new(
        DungeonMap.Parse(
        [
            "#######",
            "#.....#",
            "#.....#",
            "#######"
        ]),
        Start,
        [new Enemy(number * 10, BossAt, BossKind, Rarity.Common, level: number)],
        [],
        Stairs: StairsAt,
        Waystone: Stone);

    private static Game NewGame(Player? player = null) =>
        new(player ?? NewPlayer(), new TestFloors(() => NewFloor(1), () => NewFloor(2), () => NewFloor(3)), new FixedRandom(Dice.AlwaysFail(500)));

    private static Player NewPlayer() =>
        new(Start, new Stats(maxHealth: 40, attack: 20, defense: 5)) { Facing = Direction.Right };

    private static void ClearAndDescend(Game game)
    {
        game.MovePlayer(Direction.Right);
        game.Attack();
        Assert.True(game.StairsOpen);

        var floor = game.FloorNumber;

        for (var step = 0; step < 10 && game.FloorNumber == floor; step++)
            game.MovePlayer(Direction.Right);

        Assert.Equal(floor + 1, game.FloorNumber);
    }

    private static void ReturnToStone(Game game)
    {
        game.Player.Position = Start;
        game.Face(Direction.Down);
    }

    [Fact]
    public void DefeatingTheBoss_ClearsTheFloor_AndDeepestFloorFollowsTheHero()
    {
        var game = NewGame();

        ClearAndDescend(game);

        Assert.Equal(2, game.FloorNumber);
        Assert.Equal(2, game.DeepestFloor);
        Assert.Equal([1], game.ClearedFloors);
        Assert.Equal(Stone, game.Waystone);
    }

    [Fact]
    public void TheWaystone_TakesYouBackToACleareadFloor_WhereATotemStandsInsteadOfTheBoss()
    {
        var game = NewGame();
        ClearAndDescend(game);
        ReturnToStone(game);

        var result = game.Travel(1);

        Assert.True(result.TurnPassed);
        Assert.Contains(new Traveled(1, Start), result.Events);
        Assert.Equal(1, game.FloorNumber);
        Assert.Equal(2, game.DeepestFloor);
        Assert.DoesNotContain(game.Enemies, enemy => enemy.Kind.IsBoss);
        Assert.Equal(BossAt, game.Totem?.Position);
        Assert.True(game.StairsOpen);
        Assert.Equal([2], game.TravelDestinations);
    }

    [Fact]
    public void Travelling_NeedsTheWaystone_AndAFloorYouReached()
    {
        var game = NewGame();
        ClearAndDescend(game);
        game.Player.Position = new Position(4, 2);

        Assert.Contains(new TravelRejected(TravelResult.NoWaystoneNearby), game.Travel(1).Events);

        ReturnToStone(game);

        Assert.Contains(new TravelRejected(TravelResult.UnknownFloor), game.Travel(3).Events);
        Assert.Contains(new TravelRejected(TravelResult.UnknownFloor), game.Travel(2).Events);
        Assert.Equal(2, game.FloorNumber);
    }

    [Fact]
    public void TheTotemAndTheWaystone_BlockTheWay()
    {
        var game = NewGame();
        ClearAndDescend(game);
        ReturnToStone(game);
        game.Travel(1);

        var result = game.MovePlayer(Direction.Down);

        Assert.Equal(Start, game.Player.Position);
        Assert.Contains(new PlayerTurned(Direction.Down), result.Events);
    }

    [Fact]
    public void AwakeningTheTotem_CostsCoins_BringsTheBossBack_AndGetsPricier()
    {
        var player = NewPlayer();
        player.GainCoins(1000);
        var game = NewGame(player);
        ClearAndDescend(game);
        ReturnToStone(game);
        game.Travel(1);
        player.Position = new Position(2, 1);

        var price = game.TotemPrice;
        var coins = player.Coins;
        var result = game.AwakenTotem();

        Assert.Equal(Game.TotemPriceFor(1, 0), price);
        Assert.Contains(result.Events, e => e is TotemAwakened awakened && awakened.Price == price);
        Assert.Equal(coins - price, player.Coins);
        Assert.Null(game.Totem);
        var boss = Assert.Single(game.Enemies, enemy => enemy.Kind.IsBoss);
        Assert.Equal(BossAt, boss.Position);
        Assert.True(boss.IsAwake);
        Assert.True(Game.TotemPriceFor(1, 1) > price);
        Assert.Equal(1, game.TotemUses);
    }

    [Fact]
    public void WithoutEnoughCoins_TheTotemSleeps()
    {
        var game = NewGame();
        ClearAndDescend(game);
        ReturnToStone(game);
        game.Travel(1);
        game.Player.Position = new Position(2, 1);

        Assert.Contains(new TotemRejected(TotemResult.NotEnoughCoins), game.AwakenTotem().Events);
        Assert.NotNull(game.Totem);
    }

    [Fact]
    public void TotemPrices_GrowWithTheFloor_AndHalfAgainEachUse()
    {
        Assert.Equal(100, Game.TotemPriceFor(1, 0));
        Assert.Equal(150, Game.TotemPriceFor(1, 1));
        Assert.Equal(225, Game.TotemPriceFor(1, 2));
        Assert.Equal(200, Game.TotemPriceFor(11, 0));
    }

    [Fact]
    public void TheRiftState_SurvivesASaveAndLoad()
    {
        var player = NewPlayer();
        player.GainCoins(1000);
        var game = NewGame(player);
        ClearAndDescend(game);
        ReturnToStone(game);
        game.Travel(1);

        var content = new FakeContent().With(BossKind);
        var restored = Game.Restore(game.Capture(), content, new TestFloors(() => NewFloor(1), () => NewFloor(2), () => NewFloor(3)), new FixedRandom(Dice.AlwaysFail(50)));

        Assert.Equal(2, restored.DeepestFloor);
        Assert.Equal([1], restored.ClearedFloors);
        Assert.Equal(Stone, restored.Waystone);
        Assert.Equal(BossAt, restored.Totem?.Position);
        Assert.Equal("test-boss", restored.Totem?.Boss.Kind.Id);
    }

    [Fact]
    public void AnOldSave_CountsEveryFloorAboveAsCleared()
    {
        var game = NewGame();
        ClearAndDescend(game);
        var old = game.Capture() with { DeepestFloor = null, ClearedFloors = null, Waystone = null, Totem = null, TotemUses = 0 };

        var restored = Game.Restore(old, new FakeContent().With(BossKind), new TestFloors(() => NewFloor(1), () => NewFloor(2), () => NewFloor(3)), new FixedRandom(Dice.AlwaysFail(50)));

        Assert.Equal(2, restored.DeepestFloor);
        Assert.Equal([1], restored.ClearedFloors);
    }

    private static Game WithGraveOnFloor(Game game, int floor)
    {
        var item = new EntrySnapshot(99, new ItemSnapshot(TestItems.Armor.Id, null), 1, ItemMark.None);
        var snapshot = game.Capture() with { Grave = new GraveSnapshot(new Position(4, 2), [new GraveItemSnapshot(EquipmentSlot.Body, item)], floor) };
        return Game.Restore(snapshot, new FakeContent().With(TestItems.Armor).With(BossKind), new TestFloors(() => NewFloor(1), () => NewFloor(2), () => NewFloor(3)), new FixedRandom(Dice.AlwaysFail(500)));
    }

    [Fact]
    public void Descending_LeavesTheGraveBehindInsteadOfDestroyingIt()
    {
        var game = WithGraveOnFloor(NewGame(), floor: 1);

        ClearAndDescend(game);

        Assert.Null(game.GraveHere);
        Assert.Equal(1, game.Grave?.Floor);
        Assert.Equal(1, game.Grave?.Count);
    }

    [Fact]
    public void TravellingBackToTheGravesFloor_PutsItNearTheEntrance_ReadyToBeRecovered()
    {
        var first = NewGame();
        ClearAndDescend(first);
        var game = WithGraveOnFloor(first, floor: 1);
        ReturnToStone(game);

        var awaits = game.Travel(1).Events.OfType<GraveAwaits>().Single();

        Assert.Equal(awaits.At, game.GraveHere?.Position);
        Assert.NotEqual(Start, awaits.At);
        Assert.NotEqual(Stone, awaits.At);
    }

    private static Game AtTheTotem(int coins)
    {
        var player = NewPlayer();
        player.GainCoins(coins);
        var game = NewGame(player);
        ClearAndDescend(game);
        ReturnToStone(game);
        game.Travel(1);
        game.Player.Position = new Position(2, 1);
        game.Face(Direction.Right);
        return game;
    }

    [Fact]
    public void StrikingTheTotem_AsksFirst_AndTheSecondBlowAwakensTheBoss()
    {
        var game = AtTheTotem(coins: 1000);

        var first = game.Attack();

        Assert.False(first.TurnPassed);
        Assert.Contains(first.Events, gameEvent => gameEvent is TotemPrimed { CanAfford: true });
        Assert.NotNull(game.Totem);

        var second = game.Attack();

        Assert.Contains(second.Events, gameEvent => gameEvent is TotemAwakened);
        Assert.Null(game.Totem);
    }

    [Fact]
    public void DoingSomethingElse_CancelsTheTotemsQuestion()
    {
        var game = AtTheTotem(coins: 1000);
        game.Attack();
        game.MovePlayer(Direction.Down);
        game.MovePlayer(Direction.Up);
        game.Face(Direction.Right);

        Assert.Contains(game.Attack().Events, gameEvent => gameEvent is TotemPrimed);
        Assert.NotNull(game.Totem);
    }

    [Fact]
    public void APoorHero_LearnsThePriceButTheTotemSleeps()
    {
        var game = AtTheTotem(coins: 0);

        game.Attack();
        var result = game.Attack();

        Assert.Contains(result.Events, gameEvent => gameEvent is TotemPrimed { CanAfford: false });
        Assert.NotNull(game.Totem);
    }

    [Fact]
    public void StrikingTheWaystone_OpensTheWayWithoutSpendingATurn()
    {
        var game = NewGame();
        ClearAndDescend(game);
        ReturnToStone(game);

        var result = game.Attack();

        Assert.False(result.TurnPassed);
        Assert.Equal([new WaystoneTouched(1)], result.Events);
    }
}
