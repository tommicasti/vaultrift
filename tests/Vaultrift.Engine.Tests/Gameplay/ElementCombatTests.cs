using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class ElementCombatTests
{
    private const double NoCritical = 0.99;
    private const double Fails = 0.99;
    private const double Succeeds = 0.01;

    private static readonly DungeonMap Map = DungeonMap.Parse(
    [
        "##########",
        "#........#",
        "#........#",
        "#........#",
        "##########"
    ]);

    private static readonly EnemyKind FireWeakDummy = new(
        "dummy", "Dummy", new Stats(maxHealth: 40, attack: 1, defense: 0),
        weakness: Element.Fire, resistance: Element.Ice);

    private static readonly EnemyKind PlainDummy = new("plain", "Plain", new Stats(maxHealth: 40, attack: 1, defense: 0));

    private static readonly EnemyKind FireBreather = new(
        "breather", "Breather", new Stats(maxHealth: 40, attack: 9, defense: 0), attackElement: Element.Fire);

    private static readonly WeaponDefinition FireLegendary = new(
        "test-fire-legendary", "Test Flame", Rarity.Legendary, WeaponType.Sword,
        [new StatModifier(StatType.Attack, 3)], "Hot.", Element.Fire);

    private static readonly GearDefinition Cloak = new(
        "test-cloak", "Test Cloak", Rarity.Common, EquipmentKind.Cloak, [new StatModifier(StatType.Evasion, 0)]);

    private static Player NewPlayer(ItemInstance? weapon = null, ItemInstance? cloak = null)
    {
        var player = new Player(new Position(2, 1), new Stats(maxHealth: 30, attack: 5, defense: 2)) { Facing = Direction.Right };

        foreach (var item in new[] { weapon, cloak }.OfType<ItemInstance>())
        {
            player.Inventory.TryAdd(item);
            player.Equip(player.Inventory.Entries[^1].Id);
        }

        return player;
    }

    private static Game NewGame(Player player, Enemy[] enemies, params double[] randomValues) =>
        new(Map, player, enemies, new FixedRandom(randomValues));

    private static Enemy NewEnemy(EnemyKind kind, int x, int y, int id = 1) => new(id, new Position(x, y), kind);

    private static ItemInstance Sword(Element element) => new(TestItems.Sword, element);

    [Fact]
    public void Weakness_DealsOneAndAHalfDamage()
    {
        var enemy = NewEnemy(FireWeakDummy, 3, 1);
        var game = NewGame(NewPlayer(Sword(Element.Fire)), [enemy], NoCritical, Fails, NoCritical);

        var result = game.Attack();

        var hit = Assert.Single(result.Events.OfType<PlayerAttackedEnemy>()).Result;
        Assert.Equal(12, hit.Damage);
        Assert.Equal(Effectiveness.Effective, hit.Effectiveness);
        Assert.Equal(28, enemy.Health);
    }

    [Fact]
    public void Resistance_HalvesTheDamage()
    {
        var enemy = NewEnemy(FireWeakDummy, 3, 1);
        var game = NewGame(NewPlayer(Sword(Element.Ice)), [enemy], NoCritical, Fails, NoCritical);

        game.Attack();

        Assert.Equal(36, enemy.Health);
    }

    [Fact]
    public void LegendaryWeapon_DoublesAgainstWeakness()
    {
        var enemy = NewEnemy(FireWeakDummy, 3, 1);
        var game = NewGame(NewPlayer(new ItemInstance(FireLegendary)), [enemy], NoCritical, Fails, NoCritical);

        game.Attack();

        Assert.Equal(24, enemy.Health);
    }

    [Fact]
    public void Fire_FailedRoll_DoesNotIgnite()
    {
        var enemy = NewEnemy(FireWeakDummy, 3, 1);
        var game = NewGame(NewPlayer(Sword(Element.Fire)), [enemy], NoCritical, Fails, NoCritical);

        var result = game.Attack();

        Assert.False(enemy.IsBurning);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyIgnited or EnemyBurned);
    }

    [Fact]
    public void Fire_Ignites_AndBurnsForThreeTurns()
    {
        var kind = new EnemyKind("tough", "Tough", new Stats(maxHealth: 100, attack: 1, defense: 0), weakness: Element.Fire);
        var enemy = NewEnemy(kind, 3, 1);
        var game = NewGame(
            NewPlayer(Sword(Element.Fire)),
            [enemy],
            NoCritical, Succeeds, NoCritical,
            NoCritical, Fails, NoCritical,
            NoCritical, Fails, NoCritical,
            NoCritical, Fails, NoCritical);

        var turns = Enumerable.Range(0, 4).Select(_ => game.Attack()).ToList();

        Assert.Contains(new EnemyIgnited(1), turns[0].Events);
        Assert.Contains(new EnemyBurned(1, 2), turns[0].Events);
        Assert.Equal(3, turns.SelectMany(turn => turn.Events).Count(gameEvent => gameEvent is EnemyBurned));
        Assert.False(enemy.IsBurning);
    }

    [Fact]
    public void Fire_BurnCanFinishTheEnemy()
    {
        var kind = new EnemyKind("weak", "Weak", new Stats(maxHealth: 13, attack: 1, defense: 0), weakness: Element.Fire);
        var enemy = NewEnemy(kind, 3, 1);
        var game = NewGame(NewPlayer(Sword(Element.Fire)), [enemy], NoCritical, Succeeds, NoCritical);

        var result = game.Attack();

        Assert.True(enemy.IsDead);
        Assert.Contains(new EnemyDefeated(1), result.Events);
    }

    [Fact]
    public void Ice_Freezes_TheEnemySkipsItsTurnThenIsImmune()
    {
        var enemy = NewEnemy(PlainDummy, 3, 1);
        var game = NewGame(
            NewPlayer(Sword(Element.Ice)),
            [enemy],
            NoCritical, Succeeds,
            NoCritical, Succeeds, NoCritical);

        var frozenTurn = game.Attack();
        var nextTurn = game.Attack();

        Assert.Contains(new EnemyFrozen(1), frozenTurn.Events);
        Assert.Contains(new EnemyThawed(1), frozenTurn.Events);
        Assert.DoesNotContain(frozenTurn.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
        Assert.DoesNotContain(nextTurn.Events, gameEvent => gameEvent is EnemyFrozen);
        Assert.Contains(nextTurn.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
    }

    [Fact]
    public void Lightning_JumpsToAnAdjacentEnemyWithHalfTheDamage()
    {
        var target = NewEnemy(PlainDummy, 3, 1);
        var neighbour = NewEnemy(PlainDummy, 3, 2, id: 2);
        var game = NewGame(NewPlayer(Sword(Element.Lightning)), [target, neighbour], NoCritical, Succeeds, NoCritical);

        var result = game.Attack();

        var shock = Assert.Single(result.Events.OfType<EnemyShocked>());
        Assert.Equal(2, shock.EnemyId);
        Assert.Equal(4, shock.Result.Damage);
        Assert.Equal(36, neighbour.Health);
    }

    [Fact]
    public void Lightning_WithNobodyNearby_DoesNothingMore()
    {
        var enemy = NewEnemy(PlainDummy, 3, 1);
        var game = NewGame(NewPlayer(Sword(Element.Lightning)), [enemy], NoCritical, Succeeds, NoCritical);

        var result = game.Attack();

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyShocked);
    }

    [Fact]
    public void Wind_PushesTheEnemyOneTileAway()
    {
        var enemy = NewEnemy(PlainDummy, 3, 1);
        var game = NewGame(NewPlayer(Sword(Element.Wind)), [enemy], NoCritical, Succeeds);

        var result = game.Attack();

        Assert.Contains(new EnemyKnockedBack(1, new Position(4, 1)), result.Events);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyAttackedPlayer);
    }

    [Fact]
    public void Wind_AgainstAWall_LeavesTheEnemyInPlace()
    {
        var enemy = NewEnemy(PlainDummy, 8, 1);
        var player = NewPlayer(Sword(Element.Wind));
        player.Position = new Position(7, 1);
        var game = NewGame(player, [enemy], NoCritical, Succeeds, NoCritical);

        var result = game.Attack();

        Assert.Equal(new Position(8, 1), enemy.Position);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyKnockedBack);
    }

    [Fact]
    public void DodgedAttack_TriggersNoEffect()
    {
        var kind = new EnemyKind("nimble", "Nimble", new Stats(maxHealth: 40, attack: 1, defense: 0, evasion: 50));
        var enemy = NewEnemy(kind, 3, 1);
        var game = NewGame(NewPlayer(Sword(Element.Fire)), [enemy], 0.0, NoCritical);

        var result = game.Attack();

        Assert.True(Assert.Single(result.Events.OfType<PlayerAttackedEnemy>()).Result.IsDodged);
        Assert.False(enemy.IsBurning);
    }

    [Fact]
    public void ElementalEnemyAttack_IsHalvedByAMatchingCloak()
    {
        var enemy = NewEnemy(FireBreather, 3, 1);
        var player = NewPlayer(cloak: new ItemInstance(Cloak, Element.Fire));
        player.Facing = Direction.Down;
        var game = NewGame(player, [enemy], NoCritical);

        var result = game.Attack();

        var hit = Assert.Single(result.Events.OfType<EnemyAttackedPlayer>()).Result;
        Assert.Equal(Element.Fire, player.Resistance);
        Assert.Equal(4, hit.Damage);
        Assert.Equal(Effectiveness.Resisted, hit.Effectiveness);
    }

    [Fact]
    public void ElementalEnemyAttack_WithoutCloak_HitsNormally()
    {
        var enemy = NewEnemy(FireBreather, 3, 1);
        var player = NewPlayer();
        player.Facing = Direction.Down;
        var game = NewGame(player, [enemy], NoCritical);

        var result = game.Attack();

        Assert.Equal(7, Assert.Single(result.Events.OfType<EnemyAttackedPlayer>()).Result.Damage);
    }

    [Fact]
    public void Cloak_GoesInItsOwnSlot()
    {
        var player = NewPlayer(Sword(Element.Fire), new ItemInstance(Cloak, Element.Wind));

        Assert.True(player.Equipment.ContainsKey(EquipmentSlot.Cloak));
        Assert.True(player.Equipment.ContainsKey(EquipmentSlot.Weapon));
        Assert.Equal(Element.Wind, player.Resistance);
    }

    [Fact]
    public void PlayerRespawn_ClearsEnemyStatuses()
    {
        var enemy = NewEnemy(PlainDummy, 6, 3);
        var player = NewPlayer();
        player.Facing = Direction.Down;
        var game = NewGame(player, [enemy]);
        enemy.BurnTurnsLeft = 2;
        enemy.BurnDamage = 1;
        enemy.FreezeImmunityTurnsLeft = 2;

        player.TakeDamage(player.Health);
        game.Attack();

        Assert.False(enemy.IsBurning);
        Assert.Equal(0, enemy.FreezeImmunityTurnsLeft);
    }
}