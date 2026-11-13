using Vaultrift.Engine.Gameplay;
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
    public void CreateGame_BuildsAValidGameForEveryClass(string classId)
    {
        var playerClass = ClassCatalog.Find(classId)!;

        var game = StarterLevel.CreateGame(new SystemRandom(new Random(1)), playerClass);

        Assert.Equal(classId, game.Player.Class?.Id);
        Assert.True(game.Player.Equipment.ContainsKey(EquipmentSlot.Weapon));
        Assert.Equal("potion-healing", game.Player.QuickSlot?.Id);
        Assert.NotEmpty(game.FloorItems);
    }
}