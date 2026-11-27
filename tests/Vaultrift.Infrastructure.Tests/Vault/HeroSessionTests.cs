using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Infrastructure.Tests.Vault;

public sealed class HeroSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private static HeroSession Mage() => HeroSession.Create(Guid.NewGuid(), Guid.NewGuid(), ClassCatalog.Find("mage")!, "Elisa", Now);

    [Fact]
    public void ANewHeroStartsWithTheClassKit()
    {
        var hero = Mage();

        Assert.Equal("mage", hero.ClassId);
        Assert.Equal("Elisa", hero.Name);
        Assert.Equal(1, hero.Player.Level);
        Assert.NotEmpty(hero.Player.Equipment);
        Assert.Empty(hero.Played);
    }

    private static HeroSession Veteran()
    {
        var fresh = Mage();
        var snapshot = fresh.Player.Capture() with
        {
            Level = 6,
            UnspentPoints = 2,
            SpentPoints = new Dictionary<StatPoint, int> { [StatPoint.Mana] = 3 },
            Coins = 300,
            SpellSlots = [ItemCatalog.Scrolls[0].Id, null, null]
        };

        return HeroSession.FromSnapshot(fresh.Id, fresh.AccountId, snapshot, [Difficulty.Hardcore], 0, Now);
    }

    [Fact]
    public void AHeroSurvivesASaveRoundTrip()
    {
        var hero = Veteran();
        hero.MarkPlayed(Difficulty.Hard);
        hero.MarkPlayed(Difficulty.Normal);

        var restored = HeroSession.Restore(hero.ToEntry(Now));

        Assert.Equal(hero.Id, restored.Id);
        Assert.Equal(hero.AccountId, restored.AccountId);
        Assert.Equal(6, restored.Player.Level);
        Assert.Equal(300, restored.Player.Coins);
        Assert.Equal(hero.Player.Equipment.Keys.Order(), restored.Player.Equipment.Keys.Order());
        Assert.Equal([Difficulty.Normal, Difficulty.Hard, Difficulty.Hardcore], restored.Played);
    }

    [Fact]
    public void FallingInHardcoreKeepsTheLevelAndPointsButNothingElse()
    {
        var hero = Veteran();
        hero.Player.Inventory.TryAdd(new ItemInstance(ItemCatalog.Scrolls[1]));
        Assert.NotNull(hero.Player.LearnedSpells[0]);

        hero.LoseEverything();

        Assert.Equal(6, hero.Player.Level);
        Assert.Equal(3, hero.Player.SpentPoints[StatPoint.Mana]);
        Assert.Equal(2, hero.Player.UnspentPoints);
        Assert.Equal(0, hero.Player.Coins);
        Assert.DoesNotContain(hero.Player.Inventory.Entries, entry => entry.Item.Definition == ItemCatalog.Scrolls[1]);
        Assert.All(hero.Player.LearnedSpells, spell => Assert.Null(spell));
        Assert.Equal(Mage().Player.Equipment.Keys.Order(), hero.Player.Equipment.Keys.Order());
        Assert.Equal(hero.Player.Stats.MaxHealth, hero.Player.Health);
    }
}
