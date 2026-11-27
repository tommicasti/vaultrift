using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveFixtureTests
{
    private static readonly string[] Room =
    [
        "#############",
        "#...........#",
        "#...........#",
        "#...........#",
        "#############"
    ];

    private static readonly EnemyKind Brute = new("brute", "Brute", new Stats(maxHealth: 50, attack: 200, defense: 0, critical: 0));

    private static readonly EnemyKind Ogre = new("ogre", "Ogre", new Stats(maxHealth: 5, attack: 1, defense: 0, critical: 0), isBoss: true);

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static LiveFloor Floor(FallRule falls = FallRule.LoseCoins, bool final = false) =>
        new(DungeonMap.Parse(Room), new Position(1, 2), new NeverLucky(), floorNumber: 4) { Falls = falls, IsFinal = final };

    private static List<LiveEvent> Run(LiveFloor floor, int heroId, double moveX, double moveY, double seconds, bool attack = false)
    {
        floor.SetInput(heroId, HeroInput.Of(moveX, moveY, attack));
        var events = new List<LiveEvent>();

        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());

        floor.SetInput(heroId, HeroInput.Idle);
        return events;
    }

    private static Player ArmedPlayer(int coins = 0)
    {
        var player = new Player(new Position(1, 2), new Stats(maxHealth: 30, attack: 50, defense: 0, critical: 0), "Casti");
        player.Inventory.TryAdd(new ItemInstance(TestItems.Sword));
        player.Equip(player.Inventory.Entries[0].Id);
        player.GainCoins(coins);
        return player;
    }

    [Fact]
    public void WalkingUpToTheMerchantDoesNotOpenTheShop()
    {
        var floor = Floor();
        floor.AddHero(1, ArmedPlayer());
        floor.PlaceMerchant(new Merchant(new Position(4, 2), [new ItemInstance(TestItems.Ring)], 4));

        var events = Run(floor, 1, 1, 0, 1);

        Assert.Empty(events.OfType<LiveMerchantGreeted>());
        Assert.True(floor.IsAtMerchant(1));
    }

    [Fact]
    public void AttackingTheMerchantOpensTheShopWithoutHurtingAnyone()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, ArmedPlayer());
        floor.PlaceMerchant(new Merchant(new Position(3, 2), [new ItemInstance(TestItems.Ring)], 4));
        Run(floor, 1, 1, 0, 0.5);

        var events = Run(floor, 1, 0, 0, 0.1, attack: true);

        Assert.Single(events.OfType<LiveMerchantGreeted>());
        Assert.Equal(hero.MaxHealth, hero.Health);
    }

    [Fact]
    public void TradingNeedsTheMerchantNearby()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, ArmedPlayer(coins: 5000));
        floor.PlaceMerchant(new Merchant(new Position(9, 2), [new ItemInstance(TestItems.Ring)], 4));

        Assert.Contains(floor.Buy(1, 1).Events, e => e is TradeRejected { Reason: TradeResult.NoMerchantNearby });

        Run(floor, 1, 1, 0, 2);
        var bought = floor.Buy(1, 1);

        Assert.Contains(bought.Events, e => e is ItemBought);
        Assert.Contains(hero.Player.Inventory.Entries, entry => entry.Item.Definition == TestItems.Ring);
        Assert.Empty(floor.Merchant!.Offers);
    }

    [Fact]
    public void SellingPaysByTheFloorOfTheWorld()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, ArmedPlayer());
        hero.Player.Inventory.TryAdd(new ItemInstance(TestItems.Ring));
        var ring = hero.Player.Inventory.Entries.First(entry => entry.Item.Definition == TestItems.Ring);
        floor.PlaceMerchant(new Merchant(new Position(3, 2), Array.Empty<ItemInstance>(), 4));
        Run(floor, 1, 1, 0, 0.3);

        floor.Sell(1, ring.Id);

        Assert.Equal(ItemPrices.SellValue(TestItems.Ring, 4), hero.Player.Coins);
    }

    [Fact]
    public void TheWaystoneBlocksTheWayAndWaitsToBeStruck()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, ArmedPlayer());
        floor.PlaceWaystone(new Position(3, 2));

        var walking = Run(floor, 1, 1, 0, 2);
        var striking = Run(floor, 1, 0, 0, 0.1, attack: true);

        Assert.Empty(walking.OfType<LiveWaystoneTouched>());
        Assert.Single(striking.OfType<LiveWaystoneTouched>());
        Assert.True(floor.IsAtWaystone(1));
        Assert.True(hero.Position.X < 3 - LiveRules.FixtureRadius);
    }

    [Fact]
    public void StrikingTheAirFarFromTheWaystoneDoesNotOpenIt()
    {
        var floor = Floor();
        floor.PlaceWaystone(new Position(10, 2));
        floor.AddHero(1, ArmedPlayer());

        Assert.Empty(Run(floor, 1, 0, 0, 0.1, attack: true).OfType<LiveWaystoneTouched>());
        Assert.False(floor.IsAtWaystone(1));
    }

    [Fact]
    public void OnHardAFallenHeroLeavesTheGearInAGraveAndTakesItBack()
    {
        var floor = Floor(FallRule.DigGrave);
        var owner = Guid.NewGuid();
        var hero = floor.AddHero(1, ArmedPlayer(coins: 100), owner);
        floor.AddMonster(new Enemy(9, new Position(3, 2), Brute));

        var events = Run(floor, 1, 0, 0, 2);

        var fell = Assert.Single(events.OfType<HeroFell>());
        Assert.Equal(0, fell.CoinsLost);
        Assert.Equal(100, hero.Player.Coins);
        var grave = Assert.Single(floor.Graves);
        Assert.Equal(owner, grave.Owner);
        Assert.Contains(grave.Items, item => item.Entry.Item.Definition == TestItems.Sword);
        Assert.False(hero.Player.Equipment.ContainsKey(EquipmentSlot.MainHand) && hero.Player.Equipment[EquipmentSlot.MainHand].Item.Definition == TestItems.Sword);
    }

    [Fact]
    public void OnlyTheOwnerCanEmptyAGrave()
    {
        var floor = Floor(FallRule.DigGrave);
        var owner = Guid.NewGuid();
        var sword = new InventoryEntry(50, new ItemInstance(TestItems.Sword));
        floor.AddGrave(owner, "Casti", new Position(3, 2), [(EquipmentSlot.MainHand, sword)]);
        var stranger = floor.AddHero(1, new Player(new Position(1, 2), new Stats(maxHealth: 30, attack: 1, defense: 0, critical: 0), "Elisa"), Guid.NewGuid());
        floor.AddHero(2, new Player(new Position(1, 2), new Stats(maxHealth: 30, attack: 1, defense: 0, critical: 0), "Casti"), owner);

        floor.SetInput(2, HeroInput.Idle);
        var events = Run(floor, 1, 1, 0, 0.6);
        Assert.Single(floor.Graves);
        Assert.Empty(events.OfType<LiveGraveRecovered>());
        Assert.Empty(stranger.Player.Equipment);

        var recovered = Run(floor, 2, 1, 0, 0.6).OfType<LiveGraveRecovered>().Single();

        Assert.Equal(1, recovered.Recovered);
        Assert.Empty(floor.Graves);
    }

    [Fact]
    public void FallingAgainLosesTheOldGrave()
    {
        var floor = Floor(FallRule.DigGrave);
        var owner = Guid.NewGuid();
        var old = floor.AddGrave(owner, "Casti", new Position(10, 1), [(EquipmentSlot.Ring1, new InventoryEntry(70, new ItemInstance(TestItems.Ring)))]);
        floor.AddHero(1, ArmedPlayer(), owner);
        floor.AddMonster(new Enemy(9, new Position(3, 2), Brute));

        var events = Run(floor, 1, 0, 0, 2);

        Assert.Equal(old.Id, Assert.Single(events.OfType<LiveGraveLost>()).GraveId);
        Assert.DoesNotContain(floor.Graves, grave => grave.Id == old.Id);
    }

    [Fact]
    public void ATotemWakesItsBossOnTheSecondBlowAndTakesTheCoins()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, ArmedPlayer(coins: 300));
        floor.PlaceTotem(new Position(2, 2), new Enemy(40, new Position(2, 2), Ogre), 120);
        floor.PlaceStairs(new Position(10, 2), leadsDeeper: true);
        Assert.True(floor.StairsOpen);

        var first = Run(floor, 1, 0, 0, 0.1, attack: true);
        Assert.Equal(120, Assert.Single(first.OfType<LiveTotemPrimed>()).Price);
        Assert.Equal(300, hero.Player.Coins);

        var second = Run(floor, 1, 0, 0, 1, attack: true);

        var awakened = Assert.Single(second.OfType<LiveTotemAwakened>());
        Assert.Equal(180, hero.Player.Coins);
        Assert.Null(floor.Totem);
        Assert.Contains(floor.Monsters, monster => monster.Id == awakened.MonsterId && monster.Kind.IsBoss && monster.IsAwake);
        Assert.False(floor.StairsOpen);
    }

    [Fact]
    public void ATotemTooExpensiveOnlyShowsThePrice()
    {
        var floor = Floor();
        var hero = floor.AddHero(1, ArmedPlayer(coins: 10));
        floor.PlaceTotem(new Position(2, 2), new Enemy(40, new Position(2, 2), Ogre), 120);

        var events = Run(floor, 1, 0, 0, 2, attack: true);

        Assert.All(events.OfType<LiveTotemPrimed>(), primed => Assert.False(primed.CanAfford));
        Assert.Empty(events.OfType<LiveTotemAwakened>());
        Assert.Equal(10, hero.Player.Coins);
    }
}
