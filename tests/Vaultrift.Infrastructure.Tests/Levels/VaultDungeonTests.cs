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
        Assert.True(game.Player.Equipment.ContainsKey(EquipmentSlot.MainHand));
        Assert.DoesNotContain(game.Player.Inventory.Entries, entry => entry.Item.Definition is EquipmentDefinition);
        Assert.Equal("potion-healing", game.Player.QuickSlot?.Id);
        Assert.Equal(1, game.FloorNumber);
        Assert.Equal(VaultFloors.FloorCount, game.FloorCount);
        Assert.NotNull(game.Biome);
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
    [InlineData(VaultFloors.FloorCount)]
    public void EveryFloor_HasTheBossOfItsBiome_AndOnlyTheLastHasNoStairs(int number)
    {
        var floors = new VaultFloors(Seeded(number));
        var floor = floors.Create(number);
        var biome = floors.Biomes[number - 1];

        Assert.Equal(biome.Id, floor.Biome);
        Assert.Equal(biome.BossId, Assert.Single(floor.Enemies, enemy => enemy.Kind.IsBoss).Kind.Id);
        Assert.Equal(number < VaultFloors.FloorCount, floor.Stairs is not null);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ARun_HasABiomeForEveryFloor_NeverTheSameTwiceInARow(int seed)
    {
        var biomes = new VaultFloors(Seeded(seed)).Biomes;

        Assert.Equal(VaultFloors.FloorCount, biomes.Count);
        Assert.All(biomes.Zip(biomes.Skip(1)), pair => Assert.NotEqual(pair.First.Id, pair.Second.Id));
    }

    [Fact]
    public void ALongRun_CanRepeatBiomes()
    {
        var biomes = new VaultFloors(Seeded(1)).Biomes;

        Assert.True(biomes.Select(biome => biome.Id).Distinct().Count() < biomes.Count);
    }

    [Fact]
    public void DifferentSeeds_CanPickDifferentBiomes()
    {
        var firstBiomes = Enumerable.Range(1, 20)
            .Select(seed => new VaultFloors(Seeded(seed)).Biomes[0].Id)
            .Distinct();

        Assert.True(firstBiomes.Count() > 1);
    }

    [Fact]
    public void Bosses_GetStrongerOnDeeperFloors()
    {
        var floors = new VaultFloors(Seeded(11));
        var bosses = new[] { 1, 5, 6, 10, 11, 20 }
            .Select(number => floors.Create(number).Enemies.Single(enemy => enemy.Kind.IsBoss))
            .ToList();

        Assert.Equal(new[] { 1, 5, 6, 10, 11, 20 }, bosses.Select(boss => boss.Level));
        Assert.Equal(
            new[] { Rarity.Common, Rarity.Common, Rarity.Rare, Rarity.Rare, Rarity.Epic, Rarity.Epic },
            bosses.Select(boss => boss.Rarity));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void EveryFloor_SpawnsOnlyMonstersOfItsBiome(int seed)
    {
        var floors = new VaultFloors(Seeded(seed));

        for (var number = 1; number <= floors.Count; number++)
        {
            var biome = floors.Biomes[number - 1];

            Assert.All(
                floors.Create(number).Enemies.Where(enemy => !enemy.Kind.IsBoss),
                enemy => Assert.Contains(enemy.Kind.Id, biome.MonsterIds));
        }
    }

    [Fact]
    public void EnemiesAndItems_NeverSpawnOnObstacles()
    {
        var floors = new VaultFloors(Seeded(13));

        for (var number = 1; number <= floors.Count; number++)
        {
            var floor = floors.Create(number);

            Assert.All(floor.Enemies, enemy => Assert.True(floor.Map.IsWalkable(enemy.Position)));
            Assert.All(floor.Items, item => Assert.True(floor.Map.IsWalkable(item.Position)));
        }
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
    public void TheFirstFloor_NeverSpawnsLegendaryMonsters_AndFewEpics()
    {
        var monsters = Enumerable.Range(1, 30)
            .SelectMany(seed => new VaultFloors(Seeded(seed)).Create(1).Enemies)
            .Where(enemy => !enemy.Kind.IsBoss)
            .ToList();

        Assert.DoesNotContain(monsters, enemy => enemy.Rarity == Rarity.Legendary);
        Assert.True(monsters.Count(enemy => enemy.Rarity == Rarity.Epic) < monsters.Count / 10);
    }

    [Fact]
    public void StrongMonsters_StayUncommonOnEveryFloor()
    {
        Assert.All(Enumerable.Range(1, VaultFloors.FloorCount), floor =>
        {
            var strong = VaultFloors.MonsterRarityChance(Rarity.Epic, floor) + VaultFloors.MonsterRarityChance(Rarity.Legendary, floor);

            Assert.InRange(strong, 0.0, 0.26);
        });
    }

    [Fact]
    public void RarityChances_NeverAddUpToMoreThanOne()
    {
        Assert.All(Enumerable.Range(1, VaultFloors.FloorCount), floor =>
        {
            var rarities = new[] { Rarity.Legendary, Rarity.Epic, Rarity.Rare };

            Assert.InRange(rarities.Sum(rarity => VaultFloors.MonsterRarityChance(rarity, floor)), 0.0, 1.0);
            Assert.InRange(rarities.Sum(rarity => VaultFloors.GearRarityChance(rarity, floor)), 0.0, 1.0);
        });
    }

    [Fact]
    public void Rooms_GetMoreCrowdedOnDeeperFloors_UpToALimit()
    {
        Assert.Equal(2, VaultFloors.MaxMonstersPerRoom(1));
        Assert.True(VaultFloors.MaxMonstersPerRoom(10) > VaultFloors.MaxMonstersPerRoom(1));
        Assert.Equal(5, VaultFloors.MaxMonstersPerRoom(VaultFloors.FloorCount));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void EveryFloor_HasTwoOrThreeChests_ThatNeverBlockTheWay(int seed)
    {
        var floors = new VaultFloors(Seeded(seed));

        for (var number = 1; number <= floors.Count; number++)
        {
            var floor = floors.Create(number);
            var chests = floor.Chests!.Select(chest => chest.Position).ToHashSet();

            Assert.InRange(chests.Count, 2, 3);
            Assert.All(chests, chest => Assert.True(floor.Map.IsWalkable(chest)));

            var reachable = WalkableWithout(floor, chests);
            Assert.All(floor.Enemies, enemy => Assert.Contains(enemy.Position, reachable));
            Assert.All(floor.Items, item => Assert.Contains(item.Position, reachable));

            if (floor.Stairs is { } stairs)
                Assert.Contains(stairs, reachable);
        }
    }

    [Fact]
    public void AFloor_HidesAtMostOneMimic()
    {
        var floors = new VaultFloors(Seeded(4));

        for (var number = 1; number <= floors.Count; number++)
            Assert.InRange(floors.Create(number).Chests!.Count(chest => chest.IsMimic), 0, 1);
    }

    [Fact]
    public void Mimics_GrowMoreCommonDeeper_UpToALimit()
    {
        Assert.Equal(0.05, VaultFloors.MimicChance(1), precision: 6);
        Assert.True(VaultFloors.MimicChance(10) > VaultFloors.MimicChance(1));
        Assert.Equal(0.4, VaultFloors.MimicChance(VaultFloors.FloorCount), precision: 6);
    }

    [Fact]
    public void Mimics_HaveTheLevelOfTheirFloor_AndNeverMove()
    {
        var mimics = Enumerable.Range(1, 30)
            .Select(seed => new VaultFloors(Seeded(seed)))
            .SelectMany(floors => Enumerable.Range(1, 5).Select(number => (number, floors.Create(number))))
            .SelectMany(pair => pair.Item2.Chests!.Select(chest => (pair.number, chest.Mimic)))
            .Where(pair => pair.Mimic is not null)
            .ToList();

        Assert.NotEmpty(mimics);
        Assert.All(mimics, pair =>
        {
            Assert.Equal(pair.number, pair.Mimic!.Level);
            Assert.True(pair.Mimic.Kind.IsStationary);
        });
    }

    [Fact]
    public void LegendaryLoot_OnTheFloor_StaysRareEarlyOn()
    {
        Assert.True(VaultFloors.GearRarityChance(Rarity.Legendary, 1) < 0.01);
        Assert.True(VaultFloors.GearRarityChance(Rarity.Legendary, 10) > VaultFloors.GearRarityChance(Rarity.Legendary, 1));
        Assert.InRange(VaultFloors.GearRarityChance(Rarity.Legendary, VaultFloors.FloorCount), 0.0, 0.08);
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

    private static HashSet<Position> WalkableWithout(Floor floor, HashSet<Position> blocked)
    {
        var seen = new HashSet<Position> { floor.Start };
        var queue = new Queue<Position>([floor.Start]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var direction in Enum.GetValues<Direction>())
            {
                var next = current.Move(direction);

                if (floor.Map.IsWalkable(next) && !blocked.Contains(next) && seen.Add(next))
                    queue.Enqueue(next);
            }
        }

        return seen;
    }

    private static HashSet<Position> ReachableTiles(Floor floor) =>
        FloorGenerator.WalkingDistances(floor.Map, floor.Start).Keys.ToHashSet();
}
