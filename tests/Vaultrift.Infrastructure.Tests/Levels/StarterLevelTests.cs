using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;

namespace Vaultrift.Infrastructure.Tests.Levels;

public class StarterLevelTests
{
    [Theory]
    [InlineData("knight")]
    [InlineData("berserker")]
    [InlineData("ranger")]
    [InlineData("rogue")]
    public void CreateGame_BuildsAValidGameForEveryClass(string classId)
    {
        var playerClass = ClassCatalog.Find(classId)!;

        var game = StarterLevel.CreateGame(new SystemRandom(new Random(1)), playerClass);

        Assert.Equal(classId, game.Player.Class?.Id);
        Assert.True(game.Player.Equipment.ContainsKey(EquipmentSlot.Weapon));
        Assert.Equal("potion-healing", game.Player.QuickSlot?.Id);
        Assert.NotEmpty(game.FloorItems);
    }

    [Fact]
    public void CreateGame_UsesOnlyKindsFromTheBestiary()
    {
        var game = StarterLevel.CreateGame(new SystemRandom(new Random(1)), ClassCatalog.Find("knight")!);

        Assert.All(game.Enemies, enemy => Assert.Contains(enemy.Kind, BestiaryCatalog.All));
    }

    [Fact]
    public void CreateGame_RollsTheFloorItems_AndKeepsTheStartingKitPlain()
    {
        var game = StarterLevel.CreateGame(new SystemRandom(new Random(1)), ClassCatalog.Find("knight")!);

        Assert.All(game.Player.Inventory.Entries, entry => Assert.Null(entry.Item.Element));
        Assert.Contains(game.FloorItems.Values, item => item.Id == "cloak-common" && item.Element is not null);
        Assert.Contains(game.FloorItems.Values, item => item.Id == "sword-ember-oath" && item.Element == Element.Fire);
    }

    [Fact]
    public void CreateGame_GivesTheHeroTheChosenName()
    {
        var game = StarterLevel.CreateGame(new SystemRandom(new Random(1)), ClassCatalog.Find("rogue")!, "Tommaso");

        Assert.Equal("Tommaso", game.Player.Name);
    }
}