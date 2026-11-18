using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class HandsTests
{
    private static readonly DungeonMap OpenRoom = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#........#",
        "#........#",
        "##########"
    ]);

    private static readonly DungeonMap WalledRoom = DungeonMap.Parse(
    [
        "##########",
        "#..#.....#",
        "##########"
    ]);

    private static Player NewPlayer(int x = 2, int y = 2) =>
        new(new Position(x, y), new Stats(maxHealth: 30, attack: 5, defense: 0)) { Facing = Direction.Right };

    private static Enemy NewEnemy(int id, int x, int y) =>
        new(id, new Position(x, y), new Stats(maxHealth: 50, attack: 1, defense: 0));

    private static int DamageTo(TurnResult result, int enemyId) =>
        result.Events.OfType<PlayerAttackedEnemy>().Where(hit => hit.EnemyId == enemyId).Sum(hit => hit.Result.Damage);

    [Fact]
    public void AOneHandedWeapon_GoesToTheMainHandFirst()
    {
        var player = NewPlayer();

        player.Give(TestItems.Sword);

        Assert.Equal("test-sword", player.Equipment[EquipmentSlot.MainHand].Item.Id);
        Assert.False(player.Equipment.ContainsKey(EquipmentSlot.OffHand));
    }

    [Fact]
    public void ASecondOneHandedWeapon_GoesToTheFreeOffHand()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);

        player.Give(TestGear.Dagger);

        Assert.Equal("test-sword", player.Weapon?.Id);
        Assert.Equal("test-dagger", player.OffHandWeapon?.Id);
    }

    [Fact]
    public void AOneHandedWeapon_CanBeSentToAChosenHand()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);

        player.Give(TestGear.Dagger, EquipmentSlot.MainHand);

        Assert.Equal("test-dagger", player.Weapon?.Id);
        Assert.Contains(player.Inventory.Entries, entry => entry.Item.Id == "test-sword");
    }

    [Fact]
    public void AShield_AlwaysGoesToTheOffHand()
    {
        var player = NewPlayer();

        player.Give(TestGear.Shield());

        Assert.True(player.HasShield);
        Assert.Equal(EquipmentKind.Shield, ((GearDefinition)player.Equipment[EquipmentSlot.OffHand].Item.Definition).Kind);
    }

    [Fact]
    public void ATwoHandedWeapon_FreesBothHands()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);
        player.Give(TestGear.Shield());

        var result = player.Give(TestItems.Axe);

        Assert.Equal(EquipResult.Equipped, result);
        Assert.Equal("test-axe", player.Weapon?.Id);
        Assert.False(player.Equipment.ContainsKey(EquipmentSlot.OffHand));
        Assert.Equal(new[] { "test-shield", "test-sword" }, player.Inventory.Entries.Select(entry => entry.Item.Id).Order().ToArray());
    }

    [Fact]
    public void AShield_WhileHoldingATwoHandedWeapon_SendsTheWeaponBack()
    {
        var player = NewPlayer();
        player.Give(TestItems.Axe);

        player.Give(TestGear.Shield());

        Assert.Null(player.Weapon);
        Assert.True(player.HasShield);
        Assert.Contains(player.Inventory.Entries, entry => entry.Item.Id == "test-axe");
    }

    [Fact]
    public void ATwoHandedWeapon_WithNoRoomForBothHands_IsRejected()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);
        player.Give(TestGear.Shield());
        player.Inventory.TryAdd(TestItems.Axe);

        while (!player.Inventory.IsFull)
            player.Inventory.TryAdd(TestItems.Ring);

        var axe = player.Inventory.Entries.First(entry => entry.Item.Id == "test-axe");

        Assert.Equal(EquipResult.BackpackFull, player.Equip(axe.Id));
        Assert.Equal("test-sword", player.Weapon?.Id);
        Assert.True(player.HasShield);
    }

    [Fact]
    public void AClassMod_OfAnotherClass_IsRejected()
    {
        var knight = new CharacterClass(
            "knight", "Knight", new Stats(maxHealth: 30, attack: 5, defense: 0),
            new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Defense, 1)));
        var player = new Player(new Position(1, 1), knight);

        Assert.Equal(EquipResult.WrongClass, player.Give(TestGear.ClassMod("rogue", EffectKind.Stealth, 2)));
        Assert.Equal(EquipResult.Equipped, player.Give(TestGear.ClassMod("knight", EffectKind.Regeneration, 1)));
        Assert.True(player.Equipment.ContainsKey(EquipmentSlot.ClassMod));
    }

    [Fact]
    public void DualWielding_StrikesTwice_TheOffHandForHalfDamage()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);
        player.Give(TestGear.Dagger);
        var game = new Game(OpenRoom, player, [NewEnemy(1, 3, 2)], new FixedRandom(Dice.AlwaysFail()));

        var result = game.Attack();

        var hits = result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.Result.Damage).ToArray();
        Assert.Equal(new[] { 8, 3 }, hits);
    }

    [Fact]
    public void DualWielding_NeverAddsTheOtherWeaponToAHand()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);
        player.Give(TestGear.Dagger);

        Assert.Equal(8, player.CombatStats.Attack);
        Assert.Equal(7, player.OffHandStats.Attack);
    }

    [Fact]
    public void DualWielding_TheOffHandAppliesItsOwnElement()
    {
        var player = NewPlayer();
        player.Give(TestItems.Sword);
        player.Give(new ItemInstance(TestGear.Dagger, Element.Fire));
        var enemy = NewEnemy(1, 3, 2);
        var game = new Game(OpenRoom, player, [enemy], new FixedRandom(Dice.Then([Dice.Fails, Dice.Fails, 0.01])));

        var result = game.Attack();

        Assert.Contains(new EnemyIgnited(1), result.Events);
    }

    [Fact]
    public void Mastery_CountsOnce_WithTwoMasteredWeapons()
    {
        var rogue = new CharacterClass(
            "rogue", "Rogue", new Stats(maxHealth: 30, attack: 5, defense: 0, critical: 10),
            new WeaponMastery(WeaponType.Dagger, new StatModifier(StatType.Critical, 10)));
        var player = new Player(new Position(1, 1), rogue);

        player.Give(TestGear.Dagger);
        player.Give(TestGear.Dagger);

        Assert.Equal(20, player.CombatStats.Critical);
    }

    [Fact]
    public void Spear_HitsTheEnemyInFrontAndTheOneBehind()
    {
        var player = NewPlayer();
        player.Give(TestGear.Spear);
        var game = new Game(OpenRoom, player, [NewEnemy(1, 3, 2), NewEnemy(2, 4, 2)], new FixedRandom(Dice.AlwaysFail()));

        var result = game.Attack();

        Assert.Equal(9, DamageTo(result, 1));
        Assert.Equal(9, DamageTo(result, 2));
        Assert.Equal(new[] { new Position(3, 2), new Position(4, 2) }, result.Events.OfType<PlayerThrust>().Single().Tiles);
    }

    [Fact]
    public void Spear_ReachesAnEnemyTwoTilesAway()
    {
        var player = NewPlayer();
        player.Give(TestGear.Spear);
        var game = new Game(OpenRoom, player, [NewEnemy(1, 4, 2)], new FixedRandom(Dice.AlwaysFail()));

        Assert.Equal(9, DamageTo(game.Attack(), 1));
    }

    [Fact]
    public void Spear_CannotReachThroughAWall()
    {
        var player = NewPlayer(2, 1);
        player.Give(TestGear.Spear);
        var game = new Game(WalledRoom, player, [NewEnemy(1, 4, 1)], new FixedRandom(Dice.AlwaysFail()));

        var result = game.Attack();

        Assert.Contains(new PlayerAttackedNothing(), result.Events);
        Assert.Equal(new[] { new Position(3, 1) }, result.Events.OfType<PlayerThrust>().Single().Tiles);
    }
}
