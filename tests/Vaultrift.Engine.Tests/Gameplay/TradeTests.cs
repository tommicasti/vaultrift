using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class TradeTests
{
    private static readonly DungeonMap Hall = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#........#",
        "##########"
    ]);

    private static readonly ItemInstance RareSword =
        new(new WeaponDefinition("test-rare-sword", "Rare Sword", Rarity.Rare, WeaponType.Sword, [new StatModifier(StatType.Attack, 5)]));

    private static Player NewPlayer(int coins = 0, int x = 1, int y = 1)
    {
        var player = new Player(new Position(x, y), new Stats(maxHealth: 30, attack: 100, defense: 0)) { Facing = Direction.Right };
        player.GainCoins(coins);
        return player;
    }

    private static Game NewGame(Player player, Merchant? merchant = null, params Enemy[] enemies) =>
        new(player, new SingleFloor(new Floor(Hall, player.Position, enemies, [], Merchant: merchant)), new FixedRandom(Dice.AlwaysFail(100)));

    private static Merchant MerchantAt(int x, int y = 1, params ItemInstance[] stock) => new(new Position(x, y), stock);

    [Fact]
    public void Prices_GrowWithRarity_AndBuyingCostsFourTimesTheSale()
    {
        Assert.Equal(18, ItemPrices.SellValue(RareSword.Definition));
        Assert.Equal(72, ItemPrices.BuyPrice(RareSword.Definition));
        Assert.Equal(3, ItemPrices.SellValue(TestItems.HealingPotion));
    }

    [Fact]
    public void ADefeatedMonster_DropsCoins()
    {
        var player = NewPlayer();
        var rat = new Enemy(1, new Position(2, 1), new EnemyKind("test-rat", "Rat", new Stats(maxHealth: 5, attack: 1, defense: 0)), Rarity.Rare, level: 3);
        var game = NewGame(player, null, rat);

        var result = game.Attack();

        Assert.Contains(new CoinsGained((2 + 3 / 2) * 2), result.Events);
        Assert.Equal(6, player.Coins);
    }

    [Fact]
    public void Bosses_DropFourTimesTheCoins()
    {
        var kind = new EnemyKind("test-boss", "Boss", new Stats(maxHealth: 5, attack: 1, defense: 0), isBoss: true);

        Assert.Equal(4 * 2, Progression.CoinsFor(new Enemy(1, new Position(0, 0), kind)));
    }

    [Fact]
    public void AnUnlimitedOffer_StaysOnSale_AndCostsAQuarterMoreEachTime()
    {
        var player = NewPlayer(coins: 1000);
        var potion = new ItemInstance(TestItems.HealingPotion);
        var merchant = new Merchant(new Position(2, 1), [new StockItem(potion, OfferKind.Unlimited)]);
        var game = NewGame(player, merchant);
        var first = merchant.Offers[0].Price;

        game.Buy(merchant.Offers[0].Id);
        var second = Assert.Single(merchant.Offers);
        game.Buy(second.Id);

        Assert.Equal(Merchant.Restocked(first), second.Price);
        Assert.Equal(Merchant.Restocked(second.Price), merchant.Offers[0].Price);
        Assert.Equal(1000 - first - second.Price, player.Coins);
        Assert.Equal(2, player.Inventory.Entries.Sum(entry => entry.Quantity));
    }

    [Fact]
    public void ARegularOffer_IsGoneOnceBought()
    {
        var player = NewPlayer(coins: 1000);
        var merchant = MerchantAt(2, 1, RareSword);
        var game = NewGame(player, merchant);

        game.Buy(merchant.Offers[0].Id);

        Assert.Empty(merchant.Offers);
    }

    [Fact]
    public void Restocking_RoundsUp()
    {
        Assert.Equal(125, Merchant.Restocked(100));
        Assert.Equal(5, Merchant.Restocked(4));
    }

    [Fact]
    public void Prices_RiseTenPercentEveryFloor()
    {
        Assert.Equal(18, ItemPrices.SellValue(RareSword.Definition, 1));
        Assert.Equal(27, ItemPrices.SellValue(RareSword.Definition, 6));
        Assert.Equal(108, ItemPrices.BuyPrice(RareSword.Definition, 6));
    }

    [Fact]
    public void Dying_CostsAQuarterOfTheCoins()
    {
        var player = NewPlayer(coins: 100);
        player.Facing = Direction.Up;
        player.TakeDamage(29);
        var brute = new Enemy(1, new Position(2, 1), new Stats(maxHealth: 50, attack: 5, defense: 0)) { IsAwake = true };
        var game = NewGame(player, null, brute);

        var result = game.Attack();

        Assert.Contains(new CoinsLost(25), result.Events);
        Assert.Equal(75, player.Coins);
    }

    [Fact]
    public void WalkingIntoTheMerchant_GreetsHim_WithoutPassingTheTurn()
    {
        var game = NewGame(NewPlayer(), MerchantAt(2));

        var result = game.MovePlayer(Direction.Right);

        Assert.False(result.TurnPassed);
        Assert.Contains(new MerchantGreeted(), result.Events);
        Assert.True(game.IsNextToMerchant);
    }

    [Fact]
    public void StrikingTheMerchant_OnlyGreetsHim()
    {
        var game = NewGame(NewPlayer(), MerchantAt(2));

        var result = game.Attack();

        Assert.False(result.TurnPassed);
        Assert.Equal(new MerchantGreeted(), Assert.Single(result.Events));
    }

    [Fact]
    public void Buying_NeedsTheMerchantNextToYou()
    {
        var game = NewGame(NewPlayer(coins: 500), MerchantAt(6, 1, RareSword));

        Assert.Contains(new TradeRejected(TradeResult.NoMerchantNearby), game.Buy(1).Events);
    }

    [Fact]
    public void Buying_WithoutEnoughCoins_IsRejected()
    {
        var game = NewGame(NewPlayer(coins: 10), MerchantAt(2, 1, RareSword));

        Assert.Contains(new TradeRejected(TradeResult.NotEnoughCoins), game.Buy(1).Events);
    }

    [Fact]
    public void Buying_PutsTheItemInTheBackpack_AndTakesTheCoins()
    {
        var player = NewPlayer(coins: 100);
        var merchant = MerchantAt(2, 1, RareSword);
        var game = NewGame(player, merchant);

        var result = game.Buy(1);

        Assert.False(result.TurnPassed);
        Assert.Contains(new ItemBought(RareSword, 72), result.Events);
        Assert.Equal(28, player.Coins);
        Assert.Equal(RareSword, Assert.Single(player.Inventory.Entries).Item);
        Assert.Empty(merchant.Offers);
    }

    [Fact]
    public void Buying_WithAFullBackpack_KeepsTheCoins()
    {
        var player = NewPlayer(coins: 100);

        while (!player.Inventory.IsFull)
            player.Inventory.TryAdd(new ItemInstance(TestItems.Sword));

        var game = NewGame(player, MerchantAt(2, 1, RareSword));

        Assert.Contains(new TradeRejected(TradeResult.BackpackFull), game.Buy(1).Events);
        Assert.Equal(100, player.Coins);
    }

    [Fact]
    public void Selling_GivesItsValue_OnePieceAtATime()
    {
        var player = NewPlayer();
        player.Inventory.TryAdd(new ItemInstance(TestItems.HealingPotion));
        player.Inventory.TryAdd(new ItemInstance(TestItems.HealingPotion));
        var game = NewGame(player, MerchantAt(2));
        var entry = player.Inventory.Entries[0];

        var result = game.Sell(entry.Id);

        Assert.Contains(new ItemSold(new ItemInstance(TestItems.HealingPotion), 3), result.Events);
        Assert.Equal(3, player.Coins);
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void AFavorite_CannotBeSold()
    {
        var player = NewPlayer();
        player.Inventory.TryAdd(RareSword);
        var game = NewGame(player, MerchantAt(2));
        var entry = player.Inventory.Entries[0].Id;
        game.Mark(entry, ItemMark.Favorite);

        Assert.Contains(new TradeRejected(TradeResult.ItemIsFavorite), game.Sell(entry).Events);
    }

    [Fact]
    public void SellJunk_SellsEveryJunkPiece()
    {
        var player = NewPlayer();
        player.Inventory.TryAdd(RareSword);
        player.Inventory.TryAdd(new ItemInstance(TestItems.HealingPotion));
        player.Inventory.TryAdd(new ItemInstance(TestItems.HealingPotion));
        player.Inventory.TryAdd(new ItemInstance(TestItems.Sword));
        var game = NewGame(player, MerchantAt(2));
        game.Mark(player.Inventory.Entries[0].Id, ItemMark.Junk);
        game.Mark(player.Inventory.Entries[1].Id, ItemMark.Junk);

        var result = game.SellJunk();

        Assert.Contains(new JunkSold(3, 18 + 3 + 3), result.Events);
        Assert.Single(player.Inventory.Entries);
    }

    [Fact]
    public void TheBackpackUpgrade_AddsARow_AndCostsMoreEachTime()
    {
        var player = NewPlayer(coins: 1000);
        var game = NewGame(player, MerchantAt(2));

        game.BuyBackpackUpgrade();
        var second = game.BuyBackpackUpgrade();

        Assert.Contains(new BackpackExpanded(Inventory.DefaultCapacity + 2 * Inventory.ExpansionSize, 200), second.Events);
        Assert.Equal(700, player.Coins);
    }

    [Fact]
    public void TheBackpack_StopsGrowingAtItsLimit()
    {
        var player = NewPlayer(coins: 10_000);
        var game = NewGame(player, MerchantAt(2));

        for (var upgrade = 0; upgrade < 4; upgrade++)
            game.BuyBackpackUpgrade();

        Assert.Equal(Inventory.MaxCapacity, player.Inventory.Capacity);
        Assert.Contains(new TradeRejected(TradeResult.BackpackAtMax), game.BuyBackpackUpgrade().Events);
    }

    [Fact]
    public void Monsters_CannotWalkThroughTheMerchant()
    {
        var brute = new Enemy(1, new Position(4, 1), new Stats(maxHealth: 50, attack: 1, defense: 0)) { IsAwake = true };
        var player = NewPlayer();
        player.Facing = Direction.Up;
        var game = NewGame(player, MerchantAt(3), brute);

        game.Attack();

        Assert.NotEqual(new Position(3, 1), brute.Position);
    }

    [Fact]
    public void AMerchantOnAWall_IsInvalid()
    {
        var player = NewPlayer();

        Assert.Throws<ArgumentException>(() => NewGame(player, MerchantAt(0, 0)));
    }
}
