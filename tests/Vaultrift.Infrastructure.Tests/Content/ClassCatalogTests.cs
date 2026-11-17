using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Tests.Content;

public class ClassCatalogTests
{
    [Fact]
    public void All_IdsAreUnique()
    {
        var ids = ClassCatalog.All.Select(definition => definition.Class.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void StartingItems_ExistInTheItemCatalog()
    {
        var itemIds = ItemCatalog.All.Select(item => item.Id).ToHashSet();

        Assert.All(
            ClassCatalog.All.SelectMany(definition => definition.StartingItemIds),
            itemId => Assert.Contains(itemId, itemIds));
    }

    [Fact]
    public void EveryClass_StartsWithAWeaponItMasters()
    {
        Assert.All(ClassCatalog.All, definition => Assert.Contains(
            definition.StartingItemIds,
            itemId => ItemCatalog.Get(itemId) is WeaponDefinition weapon
                && weapon.WeaponType == definition.Class.Mastery.WeaponType));
    }

    [Fact]
    public void Find_KnownId_ReturnsTheClass()
    {
        Assert.Equal("Knight", ClassCatalog.Find("knight")?.Class.Name);
    }

    [Fact]
    public void Find_UnknownId_ReturnsNull()
    {
        Assert.Null(ClassCatalog.Find("bard"));
    }

    [Fact]
    public void Rogue_IsTheOnlyFastClass_AndCritsMoreThanTheOthers()
    {
        var rogue = ClassCatalog.Find("rogue")!.Class;

        Assert.Equal(Speed.Fast, rogue.Speed);
        Assert.All(
            ClassCatalog.All.Where(definition => definition.Class.Id != "rogue"),
            definition =>
            {
                Assert.Equal(Speed.Normal, definition.Class.Speed);
                Assert.True(definition.Class.BaseStats.Critical < rogue.BaseStats.Critical);
            });
    }
}