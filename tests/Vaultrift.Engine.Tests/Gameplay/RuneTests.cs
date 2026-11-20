using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class RuneTests
{
    private static readonly RuneDefinition IceRune = new("test-rune-ice", "Rune of Ice", Rarity.Rare, Element.Ice);

    private static readonly WeaponDefinition Staff =
        new("test-staff", "Staff", Rarity.Common, WeaponType.Staff, [new StatModifier(StatType.Attack, 2)]);

    private static readonly WeaponDefinition EmberBlade =
        new("test-ember", "Ember Blade", Rarity.Legendary, WeaponType.Sword, [new StatModifier(StatType.Attack, 9)], FixedElement: Element.Fire);

    private static readonly DungeonMap Hall = DungeonMap.Parse(
    [
        "#####",
        "#...#",
        "#####"
    ]);

    private static Player PlayerWith(ItemInstance? weapon)
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 2, defense: 0));

        if (weapon is not null)
        {
            player.Inventory.TryAdd(weapon);
            player.Equip(player.Inventory.Entries[^1].Id);
        }

        player.Inventory.TryAdd(IceRune);
        return player;
    }

    private static int RuneId(Player player) => player.Inventory.Entries.First(entry => entry.Item.Definition == IceRune).Id;

    [Fact]
    public void ARune_GivesTheWeaponInHandItsElement_AndIsUsedUp()
    {
        var player = PlayerWith(new ItemInstance(TestItems.Sword, Element.Fire));

        var result = player.Use(RuneId(player));

        Assert.Equal(UseResult.Used, result);
        Assert.Equal(Element.Ice, player.Weapon?.Element);
        Assert.Equal(TestItems.Sword, player.Weapon?.Definition);
        Assert.DoesNotContain(player.Inventory.Entries, entry => entry.Item.Definition == IceRune);
    }

    [Fact]
    public void ARune_WorksOnAPlainWeapon()
    {
        var player = PlayerWith(new ItemInstance(TestItems.Sword));

        player.Use(RuneId(player));

        Assert.Equal(Element.Ice, player.Weapon?.Element);
    }

    [Fact]
    public void WithoutAWeapon_TheRuneIsKept()
    {
        var player = PlayerWith(null);

        Assert.Equal(UseResult.NoWeaponToImbue, player.Use(RuneId(player)));
        Assert.Contains(player.Inventory.Entries, entry => entry.Item.Definition == IceRune);
    }

    [Fact]
    public void StavesAndFixedElementWeapons_CannotTakeARune()
    {
        var withStaff = PlayerWith(new ItemInstance(Staff));
        var withEmber = PlayerWith(new ItemInstance(EmberBlade));

        Assert.Equal(UseResult.WeaponCannotBeImbued, withStaff.Use(RuneId(withStaff)));
        Assert.Equal(UseResult.WeaponCannotBeImbued, withEmber.Use(RuneId(withEmber)));
        Assert.Equal(Element.Fire, withEmber.Weapon?.Element);
    }

    [Fact]
    public void ARuneOfTheSameElement_IsNotWasted()
    {
        var player = PlayerWith(new ItemInstance(TestItems.Sword, Element.Ice));

        Assert.Equal(UseResult.WeaponAlreadyImbued, player.Use(RuneId(player)));
        Assert.Contains(player.Inventory.Entries, entry => entry.Item.Definition == IceRune);
    }

    [Fact]
    public void UsingARune_TakesATurn_AndAnnouncesTheNewWeapon()
    {
        var player = PlayerWith(new ItemInstance(TestItems.Sword));
        var game = new Game(player, new SingleFloor(new Floor(Hall, player.Position, [], [])), new FixedRandom(Dice.AlwaysFail(20)));

        var result = game.UseItem(RuneId(player));

        Assert.True(result.TurnPassed);
        var imbued = Assert.Single(result.Events.OfType<WeaponImbued>());
        Assert.Equal(Element.Ice, imbued.Weapon.Element);
    }

    [Fact]
    public void Runes_Stack_AndSellLikeConsumables()
    {
        var player = PlayerWith(null);
        player.Inventory.TryAdd(IceRune);

        Assert.Equal(2, player.Inventory.Entries.Single(entry => entry.Item.Definition == IceRune).Quantity);
        Assert.Equal(ItemPrices.SellValue(new PotionDefinition("p", "P", Rarity.Rare)), ItemPrices.SellValue(IceRune));
    }
}
