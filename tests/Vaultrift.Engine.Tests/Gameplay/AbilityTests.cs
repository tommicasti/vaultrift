using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class AbilityTests
{
    private static readonly DungeonMap Hall = DungeonMap.Parse(
    [
        "############",
        "#..........#",
        "#..........#",
        "############"
    ]);

    private static CharacterClass ClassWith(ClassAbility ability, string id = "hero") =>
        new(id, "Hero", new Stats(maxHealth: 30, attack: 10, defense: 1), new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Attack, 0)),
            Ability: ability);

    private static Player NewPlayer(ClassAbility ability, string id = "hero") =>
        new(new Position(2, 1), ClassWith(ability, id)) { Facing = Direction.Right };

    private static Enemy Brute(int id, int x, int y = 1, int health = 50) =>
        new(id, new Position(x, y), new Stats(maxHealth: health, attack: 11, defense: 0));

    private static Game NewGame(Player player, params Enemy[] enemies) =>
        new(Hall, player, enemies, new FixedRandom(Dice.AlwaysFail(60)));

    private static void PassTurns(Game game, int turns)
    {
        for (var turn = 0; turn < turns; turn++)
            game.MovePlayer(turn % 2 == 0 ? Direction.Down : Direction.Up);
    }

    [Fact]
    public void AHeroWithoutAClass_HasNoAbility()
    {
        var player = new Player(new Position(2, 1), new Stats(maxHealth: 30, attack: 5, defense: 0));
        var game = NewGame(player);

        var result = game.UseAbility();

        Assert.False(result.TurnPassed);
        Assert.Contains(new AbilityRejected(AbilityResult.NoAbility), result.Events);
    }

    [Fact]
    public void Fury_RaisesAttackByHalf_WithoutPassingTheTurn()
    {
        var player = NewPlayer(ClassAbility.Fury);
        var game = NewGame(player);

        var result = game.UseAbility();

        Assert.False(result.TurnPassed);
        Assert.Contains(new AbilityUsed(ClassAbility.Fury), result.Events);
        Assert.Equal(15, player.CombatStats.Attack);
        Assert.Equal(1, player.CombatStats.Defense);
        Assert.Equal(AbilityRules.Cooldown(ClassAbility.Fury), player.AbilityCooldown);
    }

    [Fact]
    public void Fury_EndsAfterFiveTurns_AndTheCooldownKeepsTicking()
    {
        var player = NewPlayer(ClassAbility.Fury);
        var game = NewGame(player);
        game.UseAbility();

        PassTurns(game, AbilityRules.FuryTurns - 1);
        var last = game.MovePlayer(Direction.Down);

        Assert.Contains(new AbilityEnded(ClassAbility.Fury), last.Events);
        Assert.Equal(10, player.CombatStats.Attack);
        Assert.Equal(AbilityRules.Cooldown(ClassAbility.Fury) - AbilityRules.FuryTurns, player.AbilityCooldown);
    }

    [Fact]
    public void AnAbility_OnCooldown_IsRejected()
    {
        var player = NewPlayer(ClassAbility.Fury);
        var game = NewGame(player);
        game.UseAbility();

        var result = game.UseAbility();

        Assert.Contains(new AbilityRejected(AbilityResult.OnCooldown), result.Events);
    }

    [Fact]
    public void Bulwark_WithoutAShield_HalvesEveryBlow()
    {
        var player = NewPlayer(ClassAbility.Bulwark);
        player.Facing = Direction.Left;
        var game = NewGame(player, Brute(1, 3));
        game.UseAbility();

        var result = game.Attack();

        Assert.Equal(5, Assert.Single(result.Events.OfType<EnemyAttackedPlayer>()).Result.Damage);
        Assert.Equal(25, player.Health);
    }

    [Fact]
    public void Bulwark_WithAShield_StopsEveryBlow()
    {
        var player = NewPlayer(ClassAbility.Bulwark);
        player.Facing = Direction.Left;
        player.Give(TestGear.Shield(block: 1));
        var game = NewGame(player, Brute(1, 3));
        game.UseAbility();

        var result = game.Attack();

        Assert.True(Assert.Single(result.Events.OfType<EnemyAttackedPlayer>()).Result.IsBlocked);
        Assert.Equal(30, player.Health);
    }

    [Fact]
    public void Invisibility_MonstersNeitherWakeNorAttack()
    {
        var player = NewPlayer(ClassAbility.Invisibility);
        var awake = Brute(1, 3);
        awake.IsAwake = true;
        var game = NewGame(player, awake, Brute(2, 5));
        game.UseAbility();

        var result = game.MovePlayer(Direction.Down);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyAttackedPlayer or EnemyMoved or EnemyWokeUp);
        Assert.Equal(30, player.Health);
    }

    [Fact]
    public void Invisibility_TheFirstStrikeIsADoubleCritical_ThenTheHeroIsSeen()
    {
        var player = NewPlayer(ClassAbility.Invisibility);
        var game = NewGame(player, Brute(1, 3));
        game.UseAbility();

        var result = game.Attack();

        var hit = Assert.Single(result.Events.OfType<PlayerAttackedEnemy>()).Result;
        Assert.Equal(20, hit.Damage);
        Assert.True(hit.IsCritical);
        Assert.Contains(new AbilityEnded(ClassAbility.Invisibility), result.Events);
        Assert.False(player.IsInvisible);
    }

    [Fact]
    public void Assassin_StrikesFromInvisibilityForTripleDamage()
    {
        var player = NewPlayer(ClassAbility.Invisibility);
        player.Give(TestGear.ClassMod("hero", EffectKind.Ambush, 1));
        var game = NewGame(player, Brute(1, 3));
        game.UseAbility();

        var result = game.Attack();

        Assert.Equal(30, Assert.Single(result.Events.OfType<PlayerAttackedEnemy>()).Result.Damage);
    }

    [Fact]
    public void Phantom_AKillingBlowKeepsTheHeroHidden_AndLastsLonger()
    {
        var player = NewPlayer(ClassAbility.Invisibility);
        player.Give(TestGear.ClassMod("hero", EffectKind.Phantom, 2));
        var game = NewGame(player, Brute(1, 3, health: 10));
        game.UseAbility();

        Assert.Equal(AbilityRules.InvisibilityTurns + 2, player.InvisibleTurns);

        game.Attack();

        Assert.True(player.IsInvisible);
    }

    [Fact]
    public void PowerShot_WithAMeleeWeapon_BecomesADoubleDamageStrike()
    {
        var player = NewPlayer(ClassAbility.PowerShot);
        player.Give(TestItems.Sword);
        var brute = Brute(1, 3);
        var game = NewGame(player, brute);

        var result = game.UseAbility();

        Assert.True(result.TurnPassed);
        Assert.Contains(new AbilityUsed(ClassAbility.PowerShot), result.Events);
        var hit = Assert.Single(result.Events.OfType<PlayerAttackedEnemy>()).Result;
        Assert.Equal(2 * DamageCalculator.BaseDamage(player.CombatStats, brute.CombatStats), hit.Damage);
        Assert.Equal(AbilityRules.Cooldown(ClassAbility.PowerShot) - 1, player.AbilityCooldown);
    }

    [Fact]
    public void PowerShot_WithASpear_DoublesBothThrusts()
    {
        var player = NewPlayer(ClassAbility.PowerShot);
        player.Give(TestGear.Spear);
        var game = NewGame(player, Brute(1, 3), Brute(2, 4));

        var result = game.UseAbility();

        var damage = DamageCalculator.BaseDamage(player.CombatStats, new Stats(maxHealth: 50, attack: 11, defense: 0)) * 2;
        Assert.Equal(new[] { damage, damage }, result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.Result.Damage).ToArray());
    }

    [Fact]
    public void PowerShot_PiercesEveryEnemyInLine_ForDoubleDamage()
    {
        var player = NewPlayer(ClassAbility.PowerShot);
        player.Give(TestGear.Bow);
        var game = NewGame(player, Brute(1, 3), Brute(2, 6));

        var result = game.UseAbility();

        Assert.True(result.TurnPassed);
        Assert.True(result.Events.OfType<ArrowFlew>().Single().Powered);
        Assert.Equal(new[] { 26, 26 }, result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.Result.Damage).ToArray());
        Assert.Equal(AbilityRules.Cooldown(ClassAbility.PowerShot) - 1, player.AbilityCooldown);
    }

    [Fact]
    public void Volley_ShortensThePowerShotCooldownByThirtyPercent()
    {
        var player = NewPlayer(ClassAbility.PowerShot);
        player.Give(TestGear.Bow);
        player.Give(TestGear.ClassMod("hero", EffectKind.PowerShotMastery, 1));
        var game = NewGame(player, Brute(1, 5));

        game.UseAbility();

        Assert.Equal(AbilityRules.ReducedCooldown(ClassAbility.PowerShot, 30) - 1, player.AbilityCooldown);
        Assert.Equal(12, AbilityRules.ReducedCooldown(ClassAbility.PowerShot, 30));
    }

    [Fact]
    public void Haste_ShortensTheCooldown()
    {
        var player = NewPlayer(ClassAbility.Fury);
        player.Give(TestGear.Ring(EffectKind.Haste, 20));
        var game = NewGame(player);

        game.UseAbility();

        Assert.Equal(16, player.AbilityCooldown);
    }

    [Fact]
    public void Haste_StacksOnlyUpToFortyPercent()
    {
        var player = NewPlayer(ClassAbility.PowerShot);
        player.Give(TestGear.Bow);
        player.Give(TestGear.Ring(EffectKind.Haste, 20));
        player.Give(TestGear.Ring(EffectKind.Haste, 20));
        player.Give(TestGear.ClassMod("hero", EffectKind.PowerShotMastery, 1));
        var game = NewGame(player, Brute(1, 5));

        game.UseAbility();

        Assert.Equal(AbilityRules.ReducedCooldown(ClassAbility.PowerShot, 100) - 1, player.AbilityCooldown);
        Assert.Equal(10, AbilityRules.ReducedCooldown(ClassAbility.PowerShot, 100));
    }

    [Fact]
    public void Slaughterer_EveryKillDuringFuryExtendsIt()
    {
        var player = NewPlayer(ClassAbility.Fury);
        player.Give(TestGear.ClassMod("hero", EffectKind.FuryExtension, 1));
        var game = NewGame(player, Brute(1, 3, health: 5));
        game.UseAbility();

        game.Attack();

        Assert.Equal(AbilityRules.FuryTurns, player.FuryTurns);
    }

    [Fact]
    public void Dying_ClearsTheActiveAbility()
    {
        var player = NewPlayer(ClassAbility.Fury);
        var game = NewGame(player, Brute(1, 3));
        game.UseAbility();
        player.TakeDamage(29);
        player.Facing = Direction.Left;

        game.Attack();

        Assert.Equal(1, game.Deaths);
        Assert.Equal(0, player.FuryTurns);
    }
}
