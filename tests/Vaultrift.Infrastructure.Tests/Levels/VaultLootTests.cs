using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;

namespace Vaultrift.Infrastructure.Tests.Levels;

public class VaultLootTests
{
    private static readonly Rarity[] AllRarities = [Rarity.Common, Rarity.Rare, Rarity.Epic, Rarity.Legendary];

    private static Enemy Monster(Rarity rarity) =>
        new(1, new Position(1, 1), BestiaryCatalog.Get("rat"), rarity);

    private static Enemy Boss() =>
        new(1, new Position(1, 1), BestiaryCatalog.Get("rat-king"));

    private static List<ItemInstance?> RollMany(Enemy enemy, int times = 2000)
    {
        var loot = new VaultLoot(new SystemRandom(new Random(42)));
        return Enumerable.Range(0, times).Select(_ => loot.RollDrop(enemy, new LootContext("knight", Luck: 0))).ToList();
    }

    [Fact]
    public void RarerMonsters_DropMoreOften()
    {
        var chances = AllRarities.Select(rarity => VaultLoot.DropChance(Monster(rarity))).ToList();

        Assert.Equal(chances.Order().ToList(), chances);
        Assert.True(chances[0] > 0);
    }

    [Fact]
    public void Bosses_AlwaysDropEquipment()
    {
        var drops = RollMany(Boss(), times: 200);

        Assert.All(drops, drop => Assert.True(drop?.Definition is EquipmentDefinition));
        Assert.DoesNotContain(drops, drop => drop!.Rarity == Rarity.Common);
    }

    [Theory]
    [InlineData(Rarity.Common)]
    [InlineData(Rarity.Rare)]
    [InlineData(Rarity.Epic)]
    [InlineData(Rarity.Legendary)]
    public void DropRarityChances_AddUpToOne(Rarity monsterRarity)
    {
        var total = AllRarities.Sum(drop => VaultLoot.DropRarityChance(Monster(monsterRarity), drop));

        Assert.Equal(1.0, total, precision: 6);
        Assert.All(AllRarities, drop => Assert.InRange(VaultLoot.DropRarityChance(Monster(monsterRarity), drop), 0.0, 1.0));
    }

    [Fact]
    public void RarerMonsters_DropRarerItems()
    {
        var legendaryChances = AllRarities
            .Select(rarity => VaultLoot.DropRarityChance(Monster(rarity), Rarity.Legendary))
            .ToList();

        Assert.Equal(legendaryChances.Order().ToList(), legendaryChances);
        Assert.Equal(0.0, VaultLoot.DropRarityChance(Monster(Rarity.Legendary), Rarity.Common), precision: 6);
    }

    [Fact]
    public void ACommonMonster_DropsSomethingRoughlyAsOftenAsItsChance()
    {
        var drops = RollMany(Monster(Rarity.Common));
        var rate = drops.Count(drop => drop is not null) / (double)drops.Count;

        Assert.InRange(rate, VaultLoot.DropChance(Monster(Rarity.Common)) - 0.03, VaultLoot.DropChance(Monster(Rarity.Common)) + 0.03);
    }

    [Fact]
    public void MonsterDrops_MixPotionsAndEquipment()
    {
        var drops = RollMany(Monster(Rarity.Legendary)).OfType<ItemInstance>().ToList();

        Assert.Contains(drops, drop => drop.Definition is PotionDefinition);
        Assert.Contains(drops, drop => drop.Definition is EquipmentDefinition);
    }

    [Fact]
    public void Luck_RaisesTheDropChance_UpToCertainty()
    {
        var common = Monster(Rarity.Common);

        Assert.Equal(VaultLoot.DropChance(common) * 1.5, VaultLoot.DropChance(common, luck: 50), precision: 6);
        Assert.Equal(1.0, VaultLoot.DropChance(Monster(Rarity.Legendary), luck: 100), precision: 6);
    }

    [Fact]
    public void ClassMods_MostlyDropForTheHerosOwnClass()
    {
        var loot = new VaultLoot(new SystemRandom(new Random(7)));
        var mods = Enumerable.Range(0, 3000)
            .Select(_ => loot.RollDrop(Boss(), new LootContext("ranger", Luck: 0)))
            .Select(drop => drop?.Definition)
            .OfType<GearDefinition>()
            .Where(item => item.Kind == EquipmentKind.ClassMod)
            .ToList();

        var ownShare = mods.Count(mod => mod.ClassId == "ranger") / (double)mods.Count;

        Assert.NotEmpty(mods);
        Assert.InRange(ownShare, VaultLoot.OwnClassModShare - 0.07, VaultLoot.OwnClassModShare + 0.07);
    }

    [Fact]
    public void ClassMods_NeverDropAsCommons()
    {
        var drops = RollMany(Monster(Rarity.Common), times: 3000).OfType<ItemInstance>();

        Assert.DoesNotContain(drops, drop => drop.Definition is GearDefinition { Kind: EquipmentKind.ClassMod, Rarity: Rarity.Common });
    }

    [Fact]
    public void ChestRarityChances_AddUpToOne()
    {
        Assert.Equal(1.0, AllRarities.Sum(rarity => VaultLoot.ChestRarityChance(rarity)), precision: 6);
    }

    [Fact]
    public void AChest_IsNeverEmpty()
    {
        var loot = new VaultLoot(new SystemRandom(new Random(5)));

        Assert.All(Enumerable.Range(0, 300), _ => Assert.NotNull(loot.RollChest(new LootContext("knight", Luck: 0))));
    }

    [Fact]
    public void Exotics_DropOnlyFromBosses_FromFloorEight()
    {
        var loot = new VaultLoot(new SystemRandom(new Random(21)));
        var early = Enumerable.Range(0, 2000).Select(_ => loot.RollDrop(Boss(), new LootContext("knight", Luck: 0, Floor: VaultLoot.FirstExoticFloor - 1)));
        var monsters = Enumerable.Range(0, 2000).Select(_ => loot.RollDrop(Monster(Rarity.Legendary), new LootContext("knight", Luck: 0, Floor: 20)));
        var late = Enumerable.Range(0, 4000).Select(_ => loot.RollDrop(Boss(), new LootContext("knight", Luck: 0, Floor: VaultLoot.FirstExoticFloor))).ToList();

        Assert.DoesNotContain(early, item => item?.Rarity == Rarity.Exotic);
        Assert.DoesNotContain(monsters, item => item?.Rarity == Rarity.Exotic);
        Assert.InRange(late.Count(item => item?.Rarity == Rarity.Exotic) / 4000.0, 0.03, 0.07);
    }

    [Fact]
    public void Exotics_LeanTowardTheClassWeapon()
    {
        var loot = new VaultLoot(new SystemRandom(new Random(4)));
        var exotics = Enumerable.Range(0, 20000)
            .Select(_ => loot.RollDrop(Boss(), new LootContext("ranger", Luck: 0, Floor: 15)))
            .Where(item => item?.Rarity == Rarity.Exotic)
            .Select(item => (WeaponDefinition)item!.Definition)
            .ToList();

        Assert.InRange(exotics.Count(weapon => weapon.WeaponType == WeaponType.Bow) / (double)exotics.Count, 0.55, 0.80);
    }

    [Fact]
    public void Chests_SometimesHoldARune()
    {
        var loot = new VaultLoot(new SystemRandom(new Random(11)));
        var runes = Enumerable.Range(0, 3000)
            .Select(_ => loot.RollChest(new LootContext("knight", Luck: 0)))
            .Count(item => item?.Definition is RuneDefinition);

        Assert.InRange(runes / 3000.0, 0.03, 0.10);
    }

    [Fact]
    public void ThereIsOneRunePerElement()
    {
        Assert.Equal(Enum.GetValues<Element>().Order(), ItemCatalog.Runes.Select(rune => rune.Element).Order());
    }

    [Fact]
    public void AMimic_AlwaysDropsEquipment_NeverCommon()
    {
        var mimic = new Enemy(1, new Position(1, 1), BestiaryCatalog.Get("mimic"), Rarity.Rare);
        var drops = RollMany(mimic, times: 300);

        Assert.All(drops, drop => Assert.True(drop?.Definition is EquipmentDefinition));
        Assert.DoesNotContain(drops, drop => drop!.Rarity == Rarity.Common);
    }

    [Fact]
    public void StrongerBosses_DropLegendariesMoreOften()
    {
        var chances = new[] { Rarity.Common, Rarity.Rare, Rarity.Epic }
            .Select(rarity => VaultLoot.DropRarityChance(new Enemy(1, new Position(1, 1), BestiaryCatalog.Get("rat-king"), rarity), Rarity.Legendary))
            .ToList();

        Assert.Equal(chances.Order().ToList(), chances);
        Assert.True(chances[0] <= 0.05);
    }

    [Fact]
    public void ACommonMonster_AlmostNeverDropsALegendary()
    {
        Assert.True(VaultLoot.DropChance(Monster(Rarity.Common)) * VaultLoot.DropRarityChance(Monster(Rarity.Common), Rarity.Legendary) < 0.001);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void NoLegendaries_DropBeforeFloorSix(int floor)
    {
        Assert.All(AllRarities, rarity => Assert.Equal(0.0, VaultLoot.DropRarityChance(Monster(rarity), Rarity.Legendary, floor)));
        Assert.Equal(0.0, VaultLoot.DropRarityChance(Boss(), Rarity.Legendary, floor));
        Assert.Equal(0.0, VaultLoot.ChestRarityChance(Rarity.Legendary, floor));
    }

    [Fact]
    public void LegendaryChances_GrowWithTheFloor_UntilTheyAreFull()
    {
        var chances = Enumerable.Range(1, 20)
            .Select(floor => VaultLoot.DropRarityChance(Monster(Rarity.Epic), Rarity.Legendary, floor))
            .ToList();

        Assert.Equal(chances.Order().ToList(), chances);
        Assert.Equal(VaultLoot.DropRarityChance(Monster(Rarity.Epic), Rarity.Legendary), chances[^1], precision: 6);
        Assert.Equal(chances[VaultLoot.FullLegendaryFloor - 1], chances[^1], precision: 6);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(20)]
    public void DropRarityChances_AddUpToOne_OnEveryFloor(int floor)
    {
        Assert.All(AllRarities, monster =>
            Assert.Equal(1.0, AllRarities.Sum(drop => VaultLoot.DropRarityChance(Monster(monster), drop, floor)), precision: 6));
        Assert.Equal(1.0, AllRarities.Sum(drop => VaultLoot.ChestRarityChance(drop, floor)), precision: 6);
    }

    [Fact]
    public void EarlyFloors_DropNoLegendaries_EvenFromBosses()
    {
        var loot = new VaultLoot(new SystemRandom(new Random(3)));
        var drops = Enumerable.Range(0, 500).Select(_ => loot.RollDrop(Boss(), new LootContext("knight", Luck: 0, Floor: 3)));

        Assert.DoesNotContain(drops, drop => drop?.Rarity == Rarity.Legendary);
    }
}
