using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveRewardTests
{
    private static readonly string[] Room =
    [
        "#########",
        "#.......#",
        "#.......#",
        "#.......#",
        "#########"
    ];

    private static readonly EnemyKind Rat = new("rat", "Rat", new Stats(maxHealth: 5, attack: 1, defense: 0, critical: 0));

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static (LiveFloor Floor, LiveHero Hero, LiveMonster Rat) Setup(ILootTable? loot = null, int attack = 50)
    {
        var floor = new LiveFloor(DungeonMap.Parse(Room), new Position(2, 2), new NeverLucky(), loot: loot);
        var hero = floor.AddHero(1, "Casti", "knight", new Stats(maxHealth: 30, attack: attack, defense: 0, critical: 0));
        var rat = floor.AddMonster(new Enemy(10, new Position(3, 2), Rat));
        return (floor, hero, rat);
    }

    private static List<LiveEvent> Run(LiveFloor floor, double seconds)
    {
        var events = new List<LiveEvent>();
        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());
        return events;
    }

    [Fact]
    public void SlayingAMonsterPaysCoinsAndExperience()
    {
        var (floor, hero, rat) = Setup();
        var version = hero.Version;

        floor.SetInput(1, HeroInput.Of(1, 0, true));
        var events = floor.Step();

        var reward = Assert.Single(events.OfType<HeroRewarded>());
        Assert.Equal(Progression.CoinsFor(rat.Source), reward.Coins);
        Assert.Equal(reward.Coins, hero.Player.Coins);
        Assert.Equal(reward.Experience, hero.Player.Experience);
        Assert.True(hero.Version > version);
    }

    [Fact]
    public void LootFallsWhereTheMonsterDied()
    {
        var (floor, _, _) = Setup(new FixedLoot(new ItemInstance(TestItems.Sword)));

        floor.SetInput(1, HeroInput.Of(1, 0, true));
        var events = floor.Step();

        var drop = Assert.Single(floor.Items);
        Assert.Equal(new Position(3, 2), drop.Tile);
        Assert.Contains(events, e => e is LootDropped { MonsterId: 10 });
        Assert.Equal(1, floor.ItemsVersion);
    }

    [Fact]
    public void WalkingOverLootPutsItInTheBackpack()
    {
        var (floor, hero, _) = Setup(new FixedLoot(new ItemInstance(TestItems.Sword)));

        floor.SetInput(1, HeroInput.Of(1, 0, true));
        floor.Step();
        floor.SetInput(1, HeroInput.Of(1, 0, false));
        var events = Run(floor, 0.5);

        Assert.Empty(floor.Items);
        Assert.Contains(events, e => e is LootPickedUp { HeroId: 1 });
        Assert.Contains(hero.Player.Inventory.Entries, entry => entry.Item.Definition == TestItems.Sword);
    }

    [Fact]
    public void AFullBackpackLeavesTheLootOnTheFloorAndSaysSoOnce()
    {
        var (floor, hero, _) = Setup(new FixedLoot(new ItemInstance(TestItems.Sword)));
        while (hero.Player.Inventory.TryAdd(new ItemInstance(TestItems.Axe)))
        {
        }

        floor.SetInput(1, HeroInput.Of(1, 0, true));
        floor.Step();
        floor.SetInput(1, HeroInput.Of(1, 0, false));
        var events = Run(floor, 1);

        Assert.Single(floor.Items);
        Assert.Single(events.OfType<BackpackFull>());
    }

    [Fact]
    public void HeroesRegainHealthAndManaOverTime()
    {
        var floor = new LiveFloor(DungeonMap.Parse(Room), new Position(2, 2), new NeverLucky());
        var hero = floor.AddHero(1, "Casti", "knight", new Stats(maxHealth: 30, attack: 1, defense: 0, critical: 0));
        hero.Player.TakeDamage(10);
        var wounded = hero.Health;

        Run(floor, Game.HealthRegenInterval * 2);

        Assert.Equal(wounded + 2 * Game.HealthRegenAmount, hero.Health);
    }

    [Fact]
    public void BoostsWearOffAfterTheirTurnsInSeconds()
    {
        var (floor, hero, _) = Setup();
        floor.SetInput(1, HeroInput.Of(-1, 0, false));
        var potion = new ItemInstance(TestItems.StrengthPotion);
        hero.Player.Inventory.TryAdd(potion);
        var entry = hero.Player.Inventory.Entries.First(e => e.Item.Definition == TestItems.StrengthPotion);

        Assert.Equal(UseResult.Used, hero.Player.Use(entry.Id));
        var events = Run(floor, TestItems.StrengthPotion.BoostTurns + 1);

        Assert.Contains(events, e => e is BoostFaded { HeroId: 1 });
        Assert.Empty(hero.Player.Boosts);
    }
}
