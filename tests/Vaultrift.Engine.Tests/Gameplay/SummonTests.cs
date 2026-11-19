using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class SummonTests
{
    private static readonly EnemyKind Sporeling = new("test-sporeling", "Sporeling", new Stats(maxHealth: 4, attack: 1, defense: 0));

    private static readonly EnemyKind King = new("test-king", "King", new Stats(maxHealth: 60, attack: 1, defense: 0),
        isBoss: true, isStationary: true, power: BossPower.SummonMinions, minion: Sporeling);

    private static readonly string[] Hall =
    [
        "###########",
        "#.........#",
        "#.........#",
        "#.........#",
        "###########"
    ];

    private static Player NewPlayer(int x, int y, int health = 100) =>
        new(new Position(x, y), new Stats(maxHealth: health, attack: 5, defense: 0)) { Facing = Direction.Right };

    private static Game NewGame(Player player, params Enemy[] enemies) =>
        new(DungeonMap.Parse(Hall), player, enemies, new FixedRandom(Dice.AlwaysFail(200)));

    [Fact]
    public void ASummonerWithoutAMinion_CannotExist()
    {
        Assert.Throws<ArgumentException>(() =>
            new EnemyKind("test-king", "King", new Stats(maxHealth: 60, attack: 1, defense: 0), isBoss: true, power: BossPower.SummonMinions));
    }

    [Fact]
    public void TheKing_SummonsTwoAwakeMinions_EveryThirdTurn()
    {
        var king = new Enemy(1, new Position(7, 2), King) { IsAwake = true };
        var game = NewGame(NewPlayer(2, 2), king);

        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Down);
        var third = game.MovePlayer(Direction.Up);

        var summoned = third.Events.OfType<EnemySummoned>().ToList();
        Assert.Equal(2, summoned.Count);
        Assert.All(summoned, gameEvent => Assert.Equal(1, gameEvent.EnemyId));

        var minions = game.Enemies.Where(enemy => enemy.IsSummoned).ToList();
        Assert.Equal(2, minions.Count);
        Assert.All(minions, minion => Assert.True(minion.IsAwake));
        Assert.Equal(2, minions.Select(minion => minion.Id).Distinct().Count());
    }

    [Fact]
    public void TheKing_NeverKeepsMoreThanFourMinions()
    {
        var king = new Enemy(1, new Position(8, 2), King) { IsAwake = true };
        var player = NewPlayer(4, 2, health: 500);
        var game = NewGame(player, king);

        for (var turn = 0; turn < 12; turn++)
            game.MovePlayer(turn % 2 == 0 ? Direction.Up : Direction.Down);

        Assert.Equal(Game.MaxMinions, game.Enemies.Count(enemy => enemy.IsSummoned && !enemy.IsDead));
    }

    [Fact]
    public void Minions_GiveNeitherExperienceNorLoot()
    {
        var player = NewPlayer(1, 1);
        var minion = new Enemy(2, new Position(2, 1), Sporeling) { SummonerId = 1 };
        var map = DungeonMap.Parse(Hall);
        var loot = new FixedLoot(new ItemInstance(TestItems.Sword));
        var game = new Game(player, SingleFloor.From(map, player, [minion], null), new FixedRandom(Dice.AlwaysFail()), loot);

        var result = game.Attack();

        Assert.Contains(new EnemyDefeated(2), result.Events);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is ExperienceGained or ItemDropped);
    }

    [Fact]
    public void WhenTheKingFalls_HisMinionsWither()
    {
        var weakKing = new EnemyKind("test-king", "King", new Stats(maxHealth: 3, attack: 1, defense: 0),
            isBoss: true, isStationary: true, power: BossPower.SummonMinions, minion: Sporeling);
        var king = new Enemy(1, new Position(2, 1), weakKing);
        var minion = new Enemy(2, new Position(8, 3), Sporeling) { SummonerId = 1 };
        var game = NewGame(NewPlayer(1, 1), king, minion);

        var result = game.Attack();

        Assert.Contains(new EnemyWithered(2), result.Events);
        Assert.True(minion.IsDead);
    }

    [Fact]
    public void WhenTheHeroFalls_TheMinionsVanish()
    {
        var player = NewPlayer(1, 1, health: 1);
        var minion = new Enemy(2, new Position(2, 1), Sporeling) { SummonerId = 1, IsAwake = true };
        var game = NewGame(player, minion);
        player.Facing = Direction.Up;

        game.Attack();

        Assert.Equal(1, game.Deaths);
        Assert.DoesNotContain(game.Enemies, enemy => enemy.IsSummoned);
    }
}
