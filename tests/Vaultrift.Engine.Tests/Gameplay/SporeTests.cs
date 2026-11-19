using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class SporeTests
{
    private static Player NewPlayer(int x = 1, int y = 1) =>
        new(new Position(x, y), new Stats(maxHealth: 40, attack: 5, defense: 0)) { Facing = Direction.Right };

    private static Game NewGame(string[] rows, Player player) =>
        new(DungeonMap.Parse(rows), player, [], new FixedRandom(Dice.AlwaysFail()));

    [Fact]
    public void Spores_AreWalkable()
    {
        var map = DungeonMap.Parse(["####", "#.S#", "####"]);

        Assert.True(map.IsWalkable(new Position(2, 1)));
        Assert.True(map.IsSpores(new Position(2, 1)));
    }

    [Fact]
    public void EndingATurnInSpores_PoisonsTheHero()
    {
        var player = NewPlayer();
        var game = NewGame(["#####", "#.S.#", "#####"], player);

        var result = game.MovePlayer(Direction.Right);

        Assert.Contains(new PlayerPoisoned(Game.PoisonTurns), result.Events);
        Assert.Contains(new PlayerPoisonDamaged(2), result.Events);
        Assert.Equal(38, player.Health);
        Assert.Equal(Game.PoisonTurns - 1, player.PoisonTurns);
    }

    [Fact]
    public void Poison_LastsThreeTurns_ThenFades()
    {
        var player = NewPlayer();
        var game = NewGame(["#######", "#.S...#", "#######"], player);

        var events = new List<GameEvent>();

        for (var step = 0; step < 4; step++)
            events.AddRange(game.MovePlayer(Direction.Right).Events);

        Assert.Equal(3, events.OfType<PlayerPoisonDamaged>().Count());
        Assert.False(player.IsPoisoned);
    }

    [Fact]
    public void StayingInSpores_KeepsThePoisonFresh()
    {
        var player = NewPlayer();
        var game = NewGame(["#####", "#.S.#", "#####"], player);
        game.MovePlayer(Direction.Right);

        var result = game.MovePlayer(Direction.Up);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is PlayerPoisoned);
        Assert.Equal(Game.PoisonTurns - 1, player.PoisonTurns);
    }

    [Fact]
    public void AHealingPotion_CuresPoison_EvenAtFullHealth()
    {
        var player = NewPlayer();
        player.Inventory.TryAdd(new ItemInstance(TestItems.HealingPotion));
        var game = NewGame(["####", "#..#", "####"], player);
        player.PoisonTurns = 3;

        var result = game.UseItem(player.Inventory.Entries[0].Id);

        Assert.True(result.TurnPassed);
        Assert.False(player.IsPoisoned);
    }

    [Fact]
    public void ThePoison_PoundsAtLeastTwoHealth()
    {
        Assert.Equal(2, Game.PoisonDamage(10));
        Assert.Equal(5, Game.PoisonDamage(100));
    }
}
