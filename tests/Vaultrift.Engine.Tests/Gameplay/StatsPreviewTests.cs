using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class StatsPreviewTests
{
    private static readonly GearDefinition Helmet =
        new("test-helmet", "Helmet", Rarity.Rare, EquipmentKind.Head, [new StatModifier(StatType.Defense, 3)]);

    private static readonly GearDefinition Cap =
        new("test-cap", "Cap", Rarity.Common, EquipmentKind.Head, [new StatModifier(StatType.Defense, 1), new StatModifier(StatType.Evasion, 2)]);

    private static readonly FakeContent Content = new FakeContent().With(Helmet, Cap, TestItems.Sword);

    private static Player NewPlayer() => new(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 2, evasion: 4));

    private static int Add(Player player, ItemDefinition item)
    {
        player.Inventory.TryAdd(item);
        return player.Inventory.Entries[^1].Id;
    }

    [Fact]
    public void Preview_ShowsTheStatsAsIfTheItemWereWorn()
    {
        var player = NewPlayer();
        var helmet = Add(player, Helmet);

        var preview = player.PreviewEquip(helmet, null, Content);

        Assert.Equal(EquipResult.Equipped, preview.Result);
        Assert.Equal(5, preview.Defense);
        Assert.Equal(5, preview.Attack);
    }

    [Fact]
    public void Preview_ReplacesWhatIsInTheSameSlot()
    {
        var player = NewPlayer();
        player.Equip(Add(player, Helmet));
        var cap = Add(player, Cap);

        var preview = player.PreviewEquip(cap, null, Content);

        Assert.Equal(3, preview.Defense);
        Assert.Equal(6, preview.Evasion);
    }

    [Fact]
    public void Preview_DoesNotChangeTheRealHero()
    {
        var player = NewPlayer();
        var helmet = Add(player, Helmet);

        player.PreviewEquip(helmet, null, Content);

        Assert.Empty(player.Equipment);
        Assert.Equal(2, player.CombatStats.Defense);
        Assert.NotNull(player.Inventory.Find(helmet));
    }

    [Fact]
    public void Preview_OfSomethingThatCannotBeWorn_SaysWhy()
    {
        var player = NewPlayer();
        var potion = Add(player, TestItems.HealingPotion);

        var preview = player.PreviewEquip(potion, null, new FakeContent().With(TestItems.HealingPotion));

        Assert.Equal(EquipResult.NotEquippable, preview.Result);
    }

    [Fact]
    public void PreviewItem_ShowsAnItemThatIsNotInTheBackpackAsIfWorn()
    {
        var player = NewPlayer();
        player.Equip(Add(player, Helmet));

        var preview = player.PreviewItem(Cap, null, Content);

        Assert.Equal(EquipResult.Equipped, preview.Result);
        Assert.Equal(3, preview.Defense);
        Assert.Equal(6, preview.Evasion);
        Assert.Equal(Helmet, Assert.Single(player.Equipment).Value.Item.Definition);
        Assert.Empty(player.Inventory.Entries);
    }

    [Fact]
    public void PreviewItem_WorksWithAFullBackpack()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 5, defense: 2, evasion: 4));
        player.Equip(Add(player, Cap));
        while (!player.Inventory.IsFull)
            player.Inventory.TryAdd(TestItems.Sword);

        var preview = player.PreviewItem(Helmet, null, Content);

        Assert.Equal(EquipResult.Equipped, preview.Result);
        Assert.Equal(5, preview.Defense);
        Assert.True(player.Inventory.IsFull);
    }
}
