using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class PlayerTests
{
    [Fact]
    public void NewPlayer_HasFullHealthAndFacesDown()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 2));

        Assert.Equal(30, player.Health);
        Assert.False(player.IsDead);
        Assert.Equal(Direction.Down, player.Facing);
    }

    [Fact]
    public void NewPlayer_HasAnEmptyInventory()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 2));

        Assert.Empty(player.Inventory.Entries);
    }
}