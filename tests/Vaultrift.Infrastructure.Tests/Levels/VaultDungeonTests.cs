using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;

namespace Vaultrift.Infrastructure.Tests.Levels;

public class VaultDungeonTests
{
    private static IRandom Seeded(int seed) => new SystemRandom(new Random(seed));

    [Theory]
    [InlineData("knight")]
    [InlineData("berserker")]
    [InlineData("ranger")]
    [InlineData("rogue")]
    public void CreateGame_BuildsAValidGameForEveryClass(string classId)
    {
        var game = VaultDungeon.CreateGame(Seeded(1), ClassCatalog.Find(classId)!);

        Assert.Equal(classId, game.Player.Class?.Id);
        Assert.True(game.Player.Equipment.ContainsKey(EquipmentSlot.Weapon));
        Assert.DoesNotContain(game.Player.Inventory.Entries, entry => entry.Item.Definition is EquipmentDefinition);
        Assert.Equal("potion-healing", game.Player.QuickSlot?.Id);
        Assert.Equal(1, game.FloorNumber);
        Assert.Equal(VaultFloors.Themes.Count, game.FloorCount);
        Assert.NotNull(game.Stairs);
        Assert.False(game.StairsOpen);
    }

    [Fact]
    public void CreateGame_GivesTheHeroTheChosenName()
    {
        var game = VaultDungeon.CreateGame(Seeded(1), ClassCatalog.Find("rogue")!, "Tommaso");

        Assert.Equal("Tommaso", game.Player.Name);
    }

    [Fact]
    public void CreateGame_KeepsTheStartingKitPlain()
    {
        var game = VaultDungeon.CreateGame(Seeded(1), ClassCatalog.Find("knight")!);

        Assert.All(game.Player.Inventory.Entries, entry => Assert.Null(entry.Item.Element));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryFloor_HasItsBoss_AndOnlyTheLastHasNoStairs(int number)
    {
        var floor = new VaultFloors(Seeded(number)).Create(number);

        Assert.Equal(VaultFloors.Themes[number - 1].BossId, Assert.Single(floor.Enemies, enemy => enemy.Kind.IsBoss).Kind.Id);
        Assert.Equal(number < VaultFloors.Themes.Count, floor.Stairs is not null);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void EveryFloor_CanBeWalkedFromStartToBossStairsAndLoot(int seed)
    {
        var floors = new VaultFloors(Seeded(seed));

        for (var number = 1; number <= floors.Count; number++)
        {
            var floor = floors.Create(number);
            var reachable = ReachableTiles(floor);

            Assert.All(floor.Enemies, enemy => Assert.Contains(enemy.Position, reachable));
            Assert.All(floor.Items, item => Assert.Contains(item.Position, reachable));

            if (floor.Stairs is { } stairs)
                Assert.Contains(stairs, reachable);
        }
    }

    [Fact]
    public void EveryFloor_KeepsEnemiesAwayFromTheStart()
    {
        var floors = new VaultFloors(Seeded(3));

        for (var number = 1; number <= floors.Count; number++)
        {
            var floor = floors.Create(number);

            Assert.All(floor.Enemies, enemy =>
                Assert.True(Math.Abs(enemy.Position.X - floor.Start.X) + Math.Abs(enemy.Position.Y - floor.Start.Y) > 2));
        }
    }

    [Fact]
    public void DeeperFloors_SpawnHigherLevelMonsters()
    {
        var floors = new VaultFloors(Seeded(5));

        for (var number = 1; number <= floors.Count; number++)
        {
            Assert.All(
                floors.Create(number).Enemies.Where(enemy => !enemy.Kind.IsBoss),
                enemy => Assert.Equal(number, enemy.Level));
        }
    }

    [Fact]
    public void Floors_WithTheSameSeed_AreIdentical()
    {
        var first = new VaultFloors(Seeded(9)).Create(1);
        var second = new VaultFloors(Seeded(9)).Create(1);

        Assert.Equal(first.Start, second.Start);
        Assert.Equal(first.Stairs, second.Stairs);
        Assert.Equal(first.Enemies.Select(enemy => enemy.Position), second.Enemies.Select(enemy => enemy.Position));
    }

    private static HashSet<Position> ReachableTiles(Floor floor) =>
        FloorGenerator.WalkingDistances(floor.Map, floor.Start).Keys.ToHashSet();
}