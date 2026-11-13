using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Infrastructure.Levels;

namespace Vaultrift.Infrastructure.Tests.Levels;

public class StarterLevelTests
{
    [Fact]
    public void CreateGame_BuildsAValidGameWithTheStartingKit()
    {
        var game = StarterLevel.CreateGame(new SystemRandom(new Random(1)));

        Assert.True(game.Player.Equipment.ContainsKey(EquipmentSlot.Weapon));
        Assert.Equal(5, game.Player.CombatStats.Attack);
        Assert.Equal("potion-healing", game.Player.QuickSlot?.Id);
        Assert.NotEmpty(game.FloorItems);
    }
}