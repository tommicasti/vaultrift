using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class ExoticTests
{
    private static readonly string[] Hall =
    [
        "############",
        "#..........#",
        "#..........#",
        "#..........#",
        "############"
    ];

    private static readonly SpellDefinition Bolt = new("test-bolt", "Bolt", Element.Fire, SpellShape.Bolt, power: 4, manaCost: 3);

    private static WeaponDefinition Exotic(WeaponType type, ExoticPower power) =>
        new("test-exotic-" + power, power.ToString(), Rarity.Exotic, type, [new StatModifier(StatType.Attack, 5)]) { Exotic = power };

    private static WeaponDefinition ExoticStaff(ExoticPower power) =>
        new("test-exotic-" + power, power.ToString(), Rarity.Exotic, WeaponType.Staff, [new StatModifier(StatType.Attack, 5)], FixedElement: Element.Fire)
        {
            Spell = Bolt,
            Exotic = power
        };

    private static Player NewPlayer(WeaponDefinition weapon, int x = 2, int defense = 2)
    {
        var player = new Player(new Position(x, 2), new Stats(maxHealth: 60, attack: 5, defense: defense, maxMana: 30)) { Facing = Direction.Right };
        player.Give(new ItemInstance(weapon));
        return player;
    }

    private static Enemy Dummy(int id, int x, int y = 2, int health = 200, int attack = 1, int defense = 0, bool boss = false, Element? weakness = null, Element? resistance = null) =>
        new(id, new Position(x, y), new EnemyKind("dummy-" + id, "Dummy", new Stats(maxHealth: health, attack: attack, defense: defense), isBoss: boss, weakness: weakness, resistance: resistance));

    private static Game NewGame(Player player, params Enemy[] enemies) =>
        new(DungeonMap.Parse(Hall), player, enemies, new FixedRandom(Dice.AlwaysFail(400)));

    private static List<int> Damages(TurnResult result) =>
        result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.Result.Damage).ToList();

    [Fact]
    public void Riftrender_EveryThirdHitInARow_DoublesAndIgnoresDefense()
    {
        var player = NewPlayer(Exotic(WeaponType.Sword, ExoticPower.Riftrender));
        var game = NewGame(player, Dummy(1, 3, defense: 4));

        var first = game.Attack();
        var second = game.Attack();
        var third = game.Attack();

        Assert.Equal([6], Damages(first));
        Assert.Equal([6], Damages(second));
        Assert.Equal([20], Damages(third));
        Assert.Contains(third.Events, e => e is ExoticTriggered { Power: ExoticPower.Riftrender });
    }

    [Fact]
    public void Oathbound_AddsTheMeleeDamageTaken_ToTheNextAttack()
    {
        var player = NewPlayer(Exotic(WeaponType.Sword, ExoticPower.Oathbound));
        var brute = Dummy(1, 3, attack: 9);
        brute.IsAwake = true;
        var game = NewGame(player, brute);

        var first = game.Attack();
        var taken = first.Events.OfType<EnemyAttackedPlayer>().Sum(e => e.Result.Damage);
        var second = game.Attack();

        Assert.Equal(7, taken);
        Assert.Equal([10], Damages(first));
        Assert.Equal([10 + taken], Damages(second));
    }

    [Fact]
    public void HeadsmansMoon_AKillingSweep_DoesNotGiveMonstersATurn()
    {
        var player = NewPlayer(Exotic(WeaponType.Axe, ExoticPower.HeadsmansMoon));
        var victim = Dummy(1, 3, health: 3);
        var brute = Dummy(2, 2, y: 3, attack: 9);
        brute.IsAwake = true;
        var game = NewGame(player, victim, brute);

        var result = game.Attack();

        Assert.False(result.TurnPassed);
        Assert.True(victim.IsDead);
        Assert.Equal(60, player.Health);
        Assert.Contains(result.Events, e => e is ExoticTriggered { Power: ExoticPower.HeadsmansMoon });
    }

    [Fact]
    public void HeadsmansMoon_WithoutAKill_TheTurnPasses()
    {
        var player = NewPlayer(Exotic(WeaponType.Axe, ExoticPower.HeadsmansMoon));
        var game = NewGame(player, Dummy(1, 3));

        Assert.True(game.Attack().TurnPassed);
    }

    [Fact]
    public void Worldsplitter_StruckMonstersLoseTheirNextTurn_ButNotBosses()
    {
        var player = NewPlayer(Exotic(WeaponType.Axe, ExoticPower.Worldsplitter));
        var brute = Dummy(1, 3, attack: 9);
        brute.IsAwake = true;
        var game = NewGame(player, brute);

        game.Attack();

        Assert.Equal(60, player.Health);
        Assert.Equal(0, brute.StaggeredTurns);


        var boss = Dummy(5, 3, attack: 9, boss: true);
        boss.IsAwake = true;
        var bossPlayer = NewPlayer(Exotic(WeaponType.Axe, ExoticPower.Worldsplitter));
        var bossGame = NewGame(bossPlayer, boss);

        bossGame.Attack();

        Assert.True(bossPlayer.Health < 60);
    }

    [Fact]
    public void RiftNeedle_PiercesTheWholeLine_AtThreeQuartersAfterTheFirst()
    {
        var player = NewPlayer(Exotic(WeaponType.Bow, ExoticPower.RiftNeedle));
        var game = NewGame(player, Dummy(1, 4), Dummy(2, 6), Dummy(3, 8));

        var result = game.Attack();

        Assert.Equal([10, 7, 7], Damages(result));
    }

    [Fact]
    public void HuntressPromise_MarksTheTarget_ForMoreDamage_AndHealsOnAMarkedKill()
    {
        var player = NewPlayer(Exotic(WeaponType.Bow, ExoticPower.HuntressPromise));
        var target = Dummy(1, 5, health: 25);
        var game = NewGame(player, target);
        player.TakeDamage(30);

        var first = game.Attack();

        Assert.Equal([10], Damages(first));
        Assert.True(target.MarkedTurns > 0);

        var second = game.Attack();

        Assert.Equal([15], Damages(second));
        Assert.True(target.IsDead);
        Assert.Contains(second.Events, e => e is ExoticTriggered { Power: ExoticPower.HuntressPromise, Amount: 6 });
        Assert.Equal(36, player.Health);
    }

    [Fact]
    public void WidowsKiss_StacksVenom_ThatHurtsEveryTurn_UpToFive()
    {
        var player = NewPlayer(Exotic(WeaponType.Dagger, ExoticPower.WidowsKiss));
        var boss = Dummy(1, 3, boss: true);
        var game = NewGame(player, boss);

        var first = game.Attack();

        Assert.Equal(1, boss.VenomStacks);
        Assert.Contains(new EnemyEnvenomed(1, 2, 1), first.Events);

        for (var i = 0; i < 6; i++)
            game.Attack();

        Assert.Equal(Game.WidowMaxStacks, boss.VenomStacks);
    }

    [Fact]
    public void Shadowstep_LeapsToAMonsterTwoTilesAway_ForAGuaranteedCritical_ThenRecharges()
    {
        var player = NewPlayer(Exotic(WeaponType.Dagger, ExoticPower.Shadowstep));
        var target = Dummy(1, 4);
        var game = NewGame(player, target);

        var leap = game.Attack();

        Assert.Equal(new Position(3, 2), player.Position);
        Assert.Equal([20], Damages(leap));
        Assert.True(leap.Events.OfType<PlayerAttackedEnemy>().Single().Result.IsCritical);
        Assert.Equal(Game.ShadowstepCooldown - 1, game.ShadowstepCooldownLeft);

        target.Position = new Position(5, 2);
        var again = game.Attack();

        Assert.Equal(new Position(3, 2), player.Position);
        Assert.Empty(Damages(again));
    }

    [Fact]
    public void LastWall_AMonsterSteppingInFront_TakesAFreeHit()
    {
        var player = new Player(new Position(2, 3), new Stats(maxHealth: 60, attack: 5, defense: 2)) { Facing = Direction.Up };
        player.Give(new ItemInstance(Exotic(WeaponType.Spear, ExoticPower.LastWall)));
        player.Inventory.TryAdd(TestItems.HealingPotion);
        player.TakeDamage(10);
        var walker = Dummy(1, 2, y: 1);
        walker.IsAwake = true;
        var game = NewGame(player, walker);

        var result = game.UseItem(player.Inventory.Entries.First(entry => entry.Item.Definition == TestItems.HealingPotion).Id);

        Assert.Contains(new EnemyMoved(1, new Position(2, 2)), result.Events);
        Assert.Contains(result.Events, e => e is ExoticTriggered { Power: ExoticPower.LastWall });
        Assert.Equal(190, walker.Health);
    }

    [Fact]
    public void StarfallLance_ReachesThreeTiles()
    {
        var player = NewPlayer(Exotic(WeaponType.Spear, ExoticPower.StarfallLance));
        var game = NewGame(player, Dummy(1, 3), Dummy(2, 4), Dummy(3, 5), Dummy(4, 6));

        var result = game.Attack();

        Assert.Equal(3, Damages(result).Count);
    }

    [Fact]
    public void Prism_SpellsAlwaysStrikeTheWeakness()
    {
        var player = NewPlayer(ExoticStaff(ExoticPower.Prism));
        var game = NewGame(player, Dummy(1, 6, weakness: Element.Ice, resistance: Element.Fire));

        var result = game.Attack();
        var hit = result.Events.OfType<PlayerAttackedEnemy>().Single().Result;

        Assert.Equal(Effectiveness.Effective, hit.Effectiveness);
        Assert.Equal((int)(14 * ElementRules.WeaknessMultiplier(Rarity.Exotic)), hit.Damage);
    }

    [Fact]
    public void EchoStaff_CastsEverySpellTwice_TheSecondFreeAtHalfPower()
    {
        var player = NewPlayer(ExoticStaff(ExoticPower.Echo));
        var game = NewGame(player, Dummy(1, 6));

        var result = game.Attack();

        Assert.Equal([14, 7], Damages(result));
        Assert.Equal(27, player.Mana);
    }

    [Fact]
    public void AnExoticInTheOffHand_DoesNothing()
    {
        var player = new Player(new Position(2, 2), new Stats(maxHealth: 60, attack: 5, defense: 2)) { Facing = Direction.Right };
        player.Give(new ItemInstance(TestItems.Sword), EquipmentSlot.MainHand);
        player.Give(new ItemInstance(Exotic(WeaponType.Dagger, ExoticPower.WidowsKiss)), EquipmentSlot.OffHand);
        var target = Dummy(1, 3);
        var game = NewGame(player, target);

        game.Attack();

        Assert.Equal(0, target.VenomStacks);
    }
}
