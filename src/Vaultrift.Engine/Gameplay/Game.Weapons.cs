using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int BowRange = 6;
    public const int GlancingBlowDivisor = 2;
    public const int FarShotDistance = 4;

    private bool _ambushReady;
    private bool _ambushKilled;
    private bool _ambushStruck;

    private void PerformAttack(List<GameEvent> events)
    {
        if (TryOpenChest(events) || TryShatterCrystal(events) || TrySmashObstacle(events))
            return;

        BeginAmbush();

        var weapon = Player.Weapon;

        if (weapon is not null && TryShadowstep(weapon, events))
        {
            EndAmbush(events);
            return;
        }

        switch ((weapon?.Definition as WeaponDefinition)?.WeaponType)
        {
            case WeaponType.Staff when Player.ActiveSpell is not null:
                CastFromStaff(weapon!, events);
                break;
            case WeaponType.Bow:
                Shoot(weapon!, events, powered: false);

                if (RollsDoubleShot())
                    Shoot(weapon!, events, powered: false);

                break;
            case WeaponType.Axe:
                Sweep(weapon!, new HitOptions(), events);

                if (weapon!.Definition is WeaponDefinition { IsTwin: true })
                    Strike(weapon, Player.CombatStats, new HitOptions(), events);

                break;
            case WeaponType.Spear:
                Thrust(weapon!, new HitOptions(), events);
                break;
            default:
                Strike(weapon, Player.CombatStats, new HitOptions(), events);
                break;
        }

        if (Player.OffHandWeapon is { } offHand)
            Strike(offHand, Player.OffHandStats, new HitOptions(Glancing: true), events);

        EndAmbush(events);
    }

    private void Strike(ItemInstance? weapon, Stats stats, HitOptions options, List<GameEvent> events)
    {
        var target = FindLivingEnemyAt(Player.Position.Move(Player.Facing));

        if (target is null)
        {
            if (!events.OfType<PlayerAttackedNothing>().Any())
                events.Add(new PlayerAttackedNothing());

            return;
        }

        Hit(target, weapon, stats, options, events);
    }

    private void Sweep(ItemInstance weapon, HitOptions options, List<GameEvent> events)
    {
        var center = Player.Position.Move(Player.Facing);
        var sides = Perpendicular(Player.Facing).Select(center.Move).ToList();

        events.Add(new PlayerSwept([center, .. sides]));

        var target = FindLivingEnemyAt(center);
        var bystanders = sides
            .Select(FindLivingEnemyAt)
            .OfType<Enemy>()
            .ToList();

        if (target is null && bystanders.Count == 0)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        var struck = new List<Enemy>();

        if (target is not null && !Hit(target, weapon, Player.CombatStats, options, events).Missed)
            struck.Add(target);

        var sideOptions = options with { Glancing = true, TriggersElement = false, GuaranteedElement = false };

        foreach (var bystander in bystanders.Where(enemy => !enemy.IsDead))
        {
            if (!Hit(bystander, weapon, Player.CombatStats, sideOptions, events).Missed)
                struck.Add(bystander);
        }

        Stagger(struck, events);
    }

    private void Thrust(ItemInstance weapon, HitOptions options, List<GameEvent> events)
    {
        var front = Player.Position.Move(Player.Facing);
        var behind = front.Move(Player.Facing);
        var reachesBehind = (Map.IsWalkable(front) && !IsChest(front)) || FindLivingEnemyAt(front) is not null;
        var tiles = reachesBehind ? new List<Position> { front, behind } : [front];
        var beyond = behind.Move(Player.Facing);

        if (reachesBehind && Wields(Items.ExoticPower.StarfallLance) &&
            ((Map.IsWalkable(behind) && !IsChest(behind)) || FindLivingEnemyAt(behind) is not null))
            tiles.Add(beyond);

        events.Add(new PlayerThrust(tiles));

        var targets = tiles
            .Select(FindLivingEnemyAt)
            .OfType<Enemy>()
            .ToList();

        if (targets.Count == 0)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        for (var index = 0; index < targets.Count; index++)
        {
            if (!targets[index].IsDead)
                Hit(targets[index], weapon, Player.CombatStats, index == 0 ? options : options with { TriggersElement = false, GuaranteedElement = false }, events);
        }
    }

    private void Shoot(ItemInstance weapon, List<GameEvent> events, bool powered)
    {
        var path = new List<Position>();
        var targets = new List<(Enemy Enemy, int Distance)>();
        var position = Player.Position;
        var range = BowRange;
        Position? crystal = null;

        for (var step = 1; step <= range; step++)
        {
            position = position.Move(Player.Facing);
            var target = FindLivingEnemyAt(position);

            if (target is null && (!Map.IsWalkable(position) || IsChest(position)))
            {
                if (Map.IsCrystal(position))
                    crystal = position;

                break;
            }

            path.Add(position);

            if (target is null)
                continue;

            targets.Add((target, step));

            if (!powered && !Wields(Items.ExoticPower.RiftNeedle))
                break;
        }

        events.Add(new ArrowFlew(path, powered));

        if (targets.Count == 0)
        {
            if (crystal is { } lone)
                Shatter(lone, events);
            else
                events.Add(new PlayerAttackedNothing());

            return;
        }

        var guaranteedElement = powered && Player.EffectAmount(EffectKind.PowerShotMastery) > 0;

        for (var index = 0; index < targets.Count; index++)
        {
            var (target, distance) = targets[index];

            if (target.IsDead)
                continue;

            var options = new HitOptions(
                Glancing: !powered && distance == 1,
                TriggersElement: index == 0,
                DamageMultiplier: powered ? AbilityRules.PowerShotDamageMultiplier : 1,
                GuaranteedElement: guaranteedElement && index == 0,
                DamagePercent: !powered && index > 0 ? RiftNeedlePiercePercent : 100);

            Hit(target, weapon, FarShotStats(distance), options, events);
        }

        if (crystal is { } pierced)
            Shatter(pierced, events);
    }

    private void PowerStrike(List<GameEvent> events)
    {
        var weapon = Player.Weapon;
        var options = new HitOptions(
            DamageMultiplier: AbilityRules.PowerShotDamageMultiplier,
            GuaranteedElement: Player.EffectAmount(EffectKind.PowerShotMastery) > 0);

        switch ((weapon?.Definition as WeaponDefinition)?.WeaponType)
        {
            case WeaponType.Bow:
                Shoot(weapon!, events, powered: true);
                break;
            case WeaponType.Axe:
                Sweep(weapon!, options, events);
                break;
            case WeaponType.Spear:
                Thrust(weapon!, options, events);
                break;
            default:
                Strike(weapon, Player.CombatStats, options, events);
                break;
        }
    }

    private bool RollsDoubleShot()
    {
        var chance = Player.EffectAmount(EffectKind.DoubleShot);

        return chance > 0 && _random.NextDouble() < chance / 100.0;
    }

    private Stats FarShotStats(int distance)
    {
        var stats = Player.CombatStats;
        var bonus = Player.EffectAmount(EffectKind.FarShotCritical);

        return distance >= FarShotDistance && bonus > 0 ? stats.WithCritical(stats.Critical + bonus) : stats;
    }

    private AttackResult Hit(Enemy target, ItemInstance? weapon, Stats stats, HitOptions options, List<GameEvent> events)
    {
        var wasMarked = target.MarkedTurns > 0;
        options = BeforeExoticHit(target, weapon, options, events);

        var rolled = options.GuaranteedCritical
            ? new AttackResult(DamageCalculator.BaseDamage(stats, target.CombatStats) * DamageCalculator.CriticalMultiplier, IsCritical: true)
            : RollHit(stats, target, options.IgnoresDefense);

        if (!rolled.Missed && options.DamageMultiplier > 1)
            rolled = rolled with { Damage = rolled.Damage * options.DamageMultiplier };

        if (!rolled.Missed && options.DamagePercent != 100)
            rolled = rolled with { Damage = Math.Max(DamageCalculator.MinimumDamage, rolled.Damage * options.DamagePercent / 100) };

        if (!rolled.Missed && options.BonusDamage > 0)
            rolled = rolled with { Damage = rolled.Damage + options.BonusDamage };

        if (options.Glancing && !rolled.Missed)
            rolled = rolled with { Damage = Math.Max(DamageCalculator.MinimumDamage, rolled.Damage / GlancingBlowDivisor) };

        var result = ChargeInWater(ElementRules.Apply(
            rolled,
            weapon?.Element,
            target.Kind.Weakness,
            target.Kind.Resistance,
            weapon?.Rarity ?? Rarity.Common), target);

        target.TakeDamage(result.Damage);
        events.Add(new PlayerAttackedEnemy(target.Id, result));
        DrainLife(result.Damage, events);

        if (target.IsDead)
        {
            if (_ambushStruck)
                _ambushKilled = true;

            Defeat(target, events);
        }
        else
        {
            WakeUp(target, events);
        }

        if (options.TriggersElement && !result.Missed && weapon?.Element is { } element)
            TriggerElement(element, weapon.Rarity, target, result.Damage, events, options.GuaranteedElement);

        AfterExoticHit(target, weapon, result, wasMarked, events);
        return result;
    }

    private AttackResult RollHit(Stats stats, Enemy target, bool ignoresDefense = false)
    {
        var defender = ignoresDefense ? target.CombatStats.WithDefense(0) : target.CombatStats;

        if (!_ambushReady)
            return _damageCalculator.Calculate(stats, defender);

        _ambushReady = false;
        _ambushStruck = true;

        var multiplier = Player.EffectAmount(EffectKind.Ambush) > 0
            ? AbilityRules.AmbushDamageMultiplier + 1
            : AbilityRules.AmbushDamageMultiplier;

        return new AttackResult(DamageCalculator.BaseDamage(stats, defender) * multiplier, IsCritical: true);
    }

    private void DrainLife(int damage, List<GameEvent> events)
    {
        var percent = Player.EffectAmount(EffectKind.LifeSteal)
            + (Player.FuryTurns > 0 ? Player.EffectAmount(EffectKind.FuryLifeSteal) : 0);

        if (percent <= 0 || damage <= 0 || Player.Health == Player.Stats.MaxHealth)
            return;

        var amount = Math.Max(1, damage * percent / 100);
        Player.Heal(amount);
        events.Add(new LifeDrained(amount));
    }

    private void BeginAmbush()
    {
        _ambushReady = Player.IsInvisible;
        _ambushStruck = false;
        _ambushKilled = false;
    }

    private void EndAmbush(List<GameEvent> events)
    {
        _ambushReady = false;

        if (!_ambushStruck)
            return;

        if (_ambushKilled && Player.EffectAmount(EffectKind.Phantom) > 0)
            return;

        Player.InvisibleTurns = 0;
        events.Add(new AbilityEnded(ClassAbility.Invisibility));
    }

    private static void WakeUp(Enemy enemy, List<GameEvent> events)
    {
        if (enemy.IsAwake)
            return;

        enemy.IsAwake = true;
        events.Add(new EnemyWokeUp(enemy.Id));
    }

    private static Direction[] Perpendicular(Direction facing) => facing switch
    {
        Direction.Up or Direction.Down => [Direction.Left, Direction.Right],
        Direction.Left or Direction.Right => [Direction.Up, Direction.Down],
        _ => throw new ArgumentOutOfRangeException(nameof(facing), facing, "Unknown direction.")
    };

    private sealed record HitOptions(
        bool Glancing = false,
        bool TriggersElement = true,
        int DamageMultiplier = 1,
        bool GuaranteedElement = false,
        int DamagePercent = 100,
        int BonusDamage = 0,
        bool IgnoresDefense = false,
        bool GuaranteedCritical = false);
}
