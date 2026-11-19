using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class ItemEffectTests
{
    private const double Succeeds = 0.01;

    private static readonly DungeonMap LongHall = DungeonMap.Parse(
    [
        "##############",
        "#............#",
        "#............#",
        "##############"
    ]);

    private static readonly EnemyKind Rat = new("test-rat", "Rat", new Stats(maxHealth: 6, attack: 1, defense: 0));

    private static Player NewPlayer(params GearDefinition[] gear)
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 0)) { Facing = Direction.Right };

        foreach (var item in gear)
            player.Give(item);

        return player;
    }

    private static Enemy Dummy(int id, int x, int y = 1, int attack = 1) =>
        new(id, new Position(x, y), new Stats(maxHealth: 50, attack: attack, defense: 0));

    private static Game NewGame(Player player, Enemy[] enemies, params double[] firstRolls) =>
        new(LongHall, player, enemies, new FixedRandom(Dice.Then(firstRolls)));

    [Fact]
    public void LifeSteal_HealsAPartOfTheDamageDealt()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.LifeSteal, 50));
        player.TakeDamage(10);
        var game = NewGame(player, [Dummy(1, 2)]);

        var result = game.Attack();

        Assert.Contains(new LifeDrained(2), result.Events);
    }

    [Fact]
    public void LifeSteal_AtFullHealth_DoesNothing()
    {
        var game = NewGame(NewPlayer(TestGear.Ring(EffectKind.LifeSteal, 50)), [Dummy(1, 2)]);

        Assert.DoesNotContain(game.Attack().Events, gameEvent => gameEvent is LifeDrained);
    }

    [Fact]
    public void Thorns_HurtWhoeverHitsTheHero()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.Thorns, 3));
        player.Facing = Direction.Down;
        var game = NewGame(player, [Dummy(1, 2)]);

        var result = game.Attack();

        Assert.Contains(new EnemyPricked(1, 3), result.Events);
    }

    [Fact]
    public void DoubleShot_SometimesLoosesASecondArrow()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.DoubleShot, 15));
        player.Give(TestGear.Bow);
        var game = NewGame(player, [Dummy(1, 6)], Enumerable.Repeat(Succeeds, 6).ToArray());

        var result = game.Attack();

        Assert.Equal(2, result.Events.OfType<ArrowFlew>().Count());
        Assert.Equal(2, result.Events.OfType<PlayerAttackedEnemy>().Count());
    }

    [Fact]
    public void DoubleShot_WhenTheRollFails_LoosesASingleArrow()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.DoubleShot, 15));
        player.Give(TestGear.Bow);
        var game = NewGame(player, [Dummy(1, 6)]);

        var result = game.Attack();

        Assert.Single(result.Events.OfType<ArrowFlew>());
    }

    [Fact]
    public void WithoutDoubleShot_TheBowNeverRollsForIt()
    {
        var player = NewPlayer();
        player.Give(TestGear.Bow);
        var game = NewGame(player, [Dummy(1, 6)], Enumerable.Repeat(Succeeds, 6).ToArray());

        var result = game.Attack();

        Assert.Single(result.Events.OfType<ArrowFlew>());
    }

    [Fact]
    public void Wisdom_GivesMoreExperience()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.Wisdom, 50));
        player.Give(TestItems.Sword);
        var game = NewGame(player, [new Enemy(1, new Position(2, 1), Rat)]);

        var result = game.Attack();

        Assert.Contains(new ExperienceGained(5), result.Events);
    }

    [Fact]
    public void FireBoost_MakesFlamesCatchMoreOftenAndBurnHotter()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.FireBoost, 15));
        player.Give(new ItemInstance(TestItems.Sword, Element.Fire));
        var enemy = Dummy(1, 2);
        var game = NewGame(player, [enemy], Dice.Fails, 0.2);

        var result = game.Attack();

        Assert.Contains(new EnemyIgnited(1), result.Events);
        Assert.Equal(ElementRules.BurnDamage(Rarity.Common) + 1, enemy.BurnDamage);
    }

    [Fact]
    public void LightningBoost_JumpsToASecondEnemy()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.LightningBoost, 15));
        player.Give(new ItemInstance(TestItems.Sword, Element.Lightning));
        var game = NewGame(player, [Dummy(1, 2), Dummy(2, 3), Dummy(3, 4)], Dice.Fails, Succeeds);

        var result = game.Attack();

        Assert.Equal(new[] { 2, 3 }, result.Events.OfType<EnemyShocked>().Select(shock => shock.EnemyId).ToArray());
    }

    [Fact]
    public void WindBoost_PushesTwoTiles()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.WindBoost, 15));
        player.Give(new ItemInstance(TestItems.Sword, Element.Wind));
        var enemy = Dummy(1, 2);
        var game = NewGame(player, [enemy], Dice.Fails, Succeeds);

        var result = game.Attack();

        Assert.Equal(
            new[] { new Position(3, 1), new Position(4, 1) },
            result.Events.OfType<EnemyKnockedBack>().Select(push => push.To).ToArray());
    }

    [Fact]
    public void Wind_PushingIntoAnotherEnemy_SlamsForHalfTheHit()
    {
        var player = NewPlayer();
        player.Give(new ItemInstance(TestItems.Sword, Element.Wind));
        var game = NewGame(player, [Dummy(1, 2), Dummy(2, 3)], Dice.Fails, Succeeds);

        var result = game.Attack();

        Assert.Contains(new EnemySlammed(1, 4), result.Events);
    }

    [Fact]
    public void Regeneration_HealsEveryTurn()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.Regeneration, 2));
        player.TakeDamage(10);
        var game = NewGame(player, []);

        game.MovePlayer(Direction.Down);

        Assert.Equal(22, player.Health);
    }

    [Fact]
    public void Stealth_LetsTheHeroGetCloserBeforeMonstersWake()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.Stealth, 2));
        var enemy = Dummy(1, 6);
        var game = NewGame(player, [enemy]);

        game.MovePlayer(Direction.Right);

        Assert.False(enemy.IsAwake);
    }

    [Fact]
    public void Luck_ReachesTheLootTable()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.Luck, 50));
        player.Give(TestItems.Sword);
        var loot = new RecordingLoot();
        var game = new Game(player, SingleFloor.From(LongHall, player, [new Enemy(1, new Position(2, 1), Rat)], null),
            new FixedRandom(Dice.AlwaysFail()), loot);

        game.Attack();

        Assert.Equal(50, Assert.Single(loot.Contexts).Luck);
    }

    [Fact]
    public void EffectsOfDifferentItems_AddUp()
    {
        var player = NewPlayer(TestGear.Ring(EffectKind.Thorns, 2), TestGear.Ring(EffectKind.Thorns, 3));

        Assert.Equal(5, player.EffectAmount(EffectKind.Thorns));
    }

    private sealed class RecordingLoot : ILootTable
    {
        public List<LootContext> Contexts { get; } = [];

        public ItemInstance? RollDrop(Enemy enemy, LootContext context)
        {
            Contexts.Add(context);
            return null;
        }

        public ItemInstance? RollChest(LootContext context) => null;
    }
}
