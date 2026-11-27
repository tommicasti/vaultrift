using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public sealed partial class LiveFloor
{
    private readonly List<LiveProjectile> _projectiles = [];
    private readonly List<LiveEvent> _pending = [];
    private int _nextProjectileId = 1;

    private sealed record Blow(
        Stats Stats,
        ItemInstance? Weapon,
        bool Glancing = false,
        bool TriggersElement = true,
        int Multiplier = 1,
        bool Ambush = false,
        bool IgnoresDefense = false,
        int Bonus = 0,
        int DamagePercent = 100);

    public IReadOnlyList<LiveProjectile> Projectiles => _projectiles;

    public bool IsFrozen(LiveMonster monster) => Time < monster.FrozenUntil;

    public TurnResult UseAbility(int heroId)
    {
        if (_heroes.FirstOrDefault(hero => hero.Id == heroId) is not { IsDead: false } hero)
            return new TurnResult(TurnPassed: false, [new AbilityRejected(AbilityResult.NoAbility)]);

        var player = hero.Player;

        if (player.Ability is not { } ability)
            return new TurnResult(TurnPassed: false, [new AbilityRejected(AbilityResult.NoAbility)]);

        if (player.AbilityCooldown > 0)
            return new TurnResult(TurnPassed: false, [new AbilityRejected(AbilityResult.OnCooldown)]);

        var reduction = player.EffectAmount(EffectKind.Haste);

        if (ability == ClassAbility.PowerShot && player.EffectAmount(EffectKind.PowerShotMastery) > 0)
            reduction += AbilityRules.PowerShotMasteryReductionPercent;

        player.AbilityCooldown = AbilityRules.ReducedCooldown(ability, reduction);

        switch (ability)
        {
            case ClassAbility.Bulwark:
                player.BulwarkTurns = AbilityRules.BulwarkTurns + player.EffectAmount(EffectKind.BulwarkExtension);
                break;
            case ClassAbility.Fury:
                player.FuryTurns = AbilityRules.FuryTurns;
                break;
            case ClassAbility.Invisibility:
                player.InvisibleTurns = AbilityRules.InvisibilityTurns + player.EffectAmount(EffectKind.Phantom);
                foreach (var monster in _monsters.Where(monster => monster.TargetId == hero.Id))
                    monster.WindupEndsAt = null;
                break;
            case ClassAbility.Overload:
                player.IsOverloaded = true;
                break;
            case ClassAbility.PowerShot:
                Fire(hero, ProjectileKind.Arrow, LiveRules.ShotRange + 2, weapon: player.Weapon, pierces: true, multiplier: AbilityRules.PowerShotDamageMultiplier, events: _pending);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(heroId), ability, "Unknown ability.");
        }

        hero.Touch();
        _pending.Add(new AbilityActivated(hero.Id, ability));
        return new TurnResult(TurnPassed: false, [new AbilityUsed(ability)]);
    }

    public TurnResult ReadScroll(int heroId, int entryId)
    {
        if (_heroes.FirstOrDefault(hero => hero.Id == heroId) is not { IsDead: false } hero)
            return new TurnResult(TurnPassed: false, [new UseRejected(UseResult.NotUsable)]);

        var player = hero.Player;

        if (player.Inventory.Find(entryId) is not { Item.Definition: ScrollDefinition scroll } entry)
            return new TurnResult(TurnPassed: false, [new UseRejected(UseResult.NotUsable)]);

        if (player.Mana < player.SpellCost(scroll.Spell))
            return new TurnResult(TurnPassed: false, [new UseRejected(UseResult.NotEnoughMana)]);

        var item = entry.Item;
        player.Inventory.RemoveOne(entry.Id);
        Cast(hero, scroll.Spell, scroll.Rarity, _pending);
        hero.Touch();
        return new TurnResult(TurnPassed: false, [new ItemUsed(item)]);
    }

    private void Attack(LiveHero hero, List<LiveEvent> events)
    {
        var player = hero.Player;
        var profile = ExoticReach(hero, AttackProfile.For(player));

        if (hero.Input.Aim.IsZero && AutoAim(hero, profile) is { } assisted)
            hero.Facing = (assisted.Position - hero.Position).Normalized();

        hero.AttackReadyAt = Time + profile.CooldownFor(hero.Speed);
        events.Add(new HeroSwung(hero.Id));

        if (Totem is not null && AutoAim(hero, profile) is null && StrikeTotem(hero, events))
            return;

        if (AutoAim(hero, profile) is null && (OpenChest(hero, events) || Hail(hero, events)))
            return;

        if (profile.Style != AttackStyle.Shoot && AutoAim(hero, profile) is null && (TryShadowstep(hero, profile, events) || TryBreakTerrain(hero, events)))
            return;

        var ambush = player.IsInvisible;

        switch (profile.Style)
        {
            case AttackStyle.Shoot:
                var pierces = (player.Weapon?.Definition as WeaponDefinition)?.Pierces == true || hero.Wields(ExoticPower.RiftNeedle);
                Fire(hero, ProjectileKind.Arrow, LiveRules.ShotRange, weapon: player.Weapon, pierces: pierces, ambush: ambush, events: events);

                if (player.EffectAmount(EffectKind.DoubleShot) is > 0 and var chance && _random.NextDouble() < chance / 100.0)
                    Fire(hero, ProjectileKind.Arrow, LiveRules.ShotRange, weapon: player.Weapon, pierces: pierces, events: events, direction: hero.Facing.Rotated(LiveRules.DoubleShotSpread));

                break;
            case AttackStyle.Cast when player.Mana >= player.SpellCost(player.ActiveSpell!):
                Cast(hero, player.ActiveSpell!, player.Weapon?.Rarity ?? Rarity.Common, events);

                if (hero.Wields(ExoticPower.Echo))
                {
                    events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.Echo));
                    _spellPercent = Game.EchoPowerPercent;
                    Cast(hero, player.ActiveSpell!, player.Weapon?.Rarity ?? Rarity.Common, events, echo: true);
                    _spellPercent = 100;
                }

                break;
            case AttackStyle.Cast:
                Melee(hero, AttackProfile.Strike, new Blow(player.CombatStats, player.Weapon, Glancing: true, TriggersElement: false), events);
                break;
            default:
                Melee(hero, profile, new Blow(player.CombatStats, player.Weapon, Ambush: ambush), events);
                break;
        }

        if (ambush)
        {
            player.InvisibleTurns = 0;
            hero.Touch();
            events.Add(new AbilityFaded(hero.Id, ClassAbility.Invisibility));
        }
    }

    private LiveMonster? AutoAim(LiveHero hero, AttackProfile profile)
    {
        var reachable = _monsters
            .Where(monster => !monster.IsDead && monster.Position.DistanceTo(hero.Position) <= profile.Reach + monster.Radius)
            .Where(monster => !profile.IsRanged || Collision.ClearPath(Map, hero.Position, monster.Position, 0.1))
            .OrderBy(monster => monster.Position.DistanceTo(hero.Position))
            .ToList();

        var heading = hero.Input.Move.IsZero ? hero.Facing : hero.Input.Move.Normalized();

        return reachable.FirstOrDefault(monster => InAimCone(hero, heading, monster)) ?? reachable.FirstOrDefault();
    }

    private static bool InAimCone(LiveHero hero, Vector heading, LiveMonster monster)
    {
        var offset = monster.Position - hero.Position;
        var distance = offset.Length;
        return distance < 1e-6 || heading.Dot(offset * (1 / distance)) >= LiveRules.AimConeCosine;
    }

    private void Melee(LiveHero hero, AttackProfile profile, Blow blow, List<LiveEvent> events)
    {
        var targets = _monsters
            .Where(monster => !monster.IsDead && InArc(hero, monster, profile))
            .OrderBy(monster => monster.Position.DistanceTo(hero.Position))
            .Take(profile.MaxTargets)
            .ToList();

        if (targets.Count == 0)
            return;

        var main = targets[0];
        HitMonster(hero, main, blow, events);

        foreach (var bystander in targets.Skip(1))
        {
            var side = profile.Style == AttackStyle.Sweep ? blow with { Glancing = true, TriggersElement = false, Ambush = false } : blow with { Ambush = false };
            HitMonster(hero, bystander, side, events);
        }

        var player = hero.Player;

        if (player.Weapon?.Definition is WeaponDefinition { IsTwin: true } && !main.IsDead)
            HitMonster(hero, main, blow with { Ambush = false }, events);

        if (player.OffHandWeapon is { } offHand && !main.IsDead)
            HitMonster(hero, main, new Blow(player.OffHandStats, offHand, Glancing: true), events);

        Stagger(hero, targets, events);
    }

    private static bool InArc(LiveHero hero, LiveMonster monster, AttackProfile profile)
    {
        var offset = monster.Position - hero.Position;
        var distance = offset.Length;

        if (distance > profile.Reach + monster.Radius)
            return false;

        return distance < 1e-6 || hero.Facing.Dot(offset * (1 / distance)) >= profile.ArcCosine;
    }

    private void Fire(LiveHero hero, ProjectileKind kind, double range, List<LiveEvent> events, ItemInstance? weapon = null, SpellDefinition? spell = null, Rarity rarity = Rarity.Common, int size = 0, bool pierces = false, int multiplier = 1, bool ambush = false, Vector? direction = null)
    {
        var heading = direction ?? hero.Facing;
        var ambushMultiplier = AbilityRules.AmbushDamageMultiplier + (hero.Player.EffectAmount(EffectKind.Ambush) > 0 ? 1 : 0);
        var projectile = new LiveProjectile(_nextProjectileId++, hero.Id, kind, hero.Position + heading * LiveRules.HeroRadius, heading, kind == ProjectileKind.Arrow ? LiveRules.ArrowSpeed : LiveRules.SpellSpeed, range)
        {
            PowerPercent = _spellPercent,
            PiercePercent = hero.Wields(ExoticPower.RiftNeedle) ? Game.RiftNeedlePiercePercent : LiveRules.PiercePercent,
            Weapon = weapon,
            Spell = spell,
            Rarity = spell is null ? weapon?.Rarity ?? Rarity.Common : rarity,
            Size = size,
            Pierces = pierces,
            Multiplier = ambush ? multiplier * ambushMultiplier : multiplier
        };

        _projectiles.Add(projectile);
        events.Add(new ShotFired(hero.Id, projectile.Id));
    }

    private void MoveProjectiles(List<LiveEvent> events)
    {
        foreach (var projectile in _projectiles.ToList())
        {
            var owner = _heroes.FirstOrDefault(hero => hero.Id == projectile.OwnerId);

            if (owner is null)
            {
                _projectiles.Remove(projectile);
                continue;
            }

            var travel = projectile.Speed * LiveRules.TickSeconds;
            var steps = (int)Math.Ceiling(travel / LiveRules.ProjectileStep);
            var done = false;

            for (var step = 0; step < steps && !done; step++)
            {
                var advance = Math.Min(LiveRules.ProjectileStep, projectile.RangeLeft);
                var next = projectile.Position + projectile.Direction * advance;
                projectile.RangeLeft -= advance;

                if (Collision.HitsWall(Map, next, LiveRules.ProjectileRadius))
                {
                    if (projectile.Kind == ProjectileKind.Arrow && Map.IsCrystal(Collision.TileOf(next)))
                        Shatter(owner, Collision.TileOf(next), events);

                    Land(owner, projectile, projectile.Position, null, events);
                    done = true;
                    break;
                }

                projectile.Position = next;

                var victim = _monsters.FirstOrDefault(monster =>
                    !monster.IsDead && !projectile.Struck.Contains(monster.Id) && monster.Position.DistanceTo(next) <= monster.Radius + LiveRules.ProjectileRadius);

                if (victim is not null)
                    done = Strike(owner, projectile, victim, events);
                else if (projectile.RangeLeft <= 0)
                {
                    Land(owner, projectile, projectile.Position, null, events);
                    done = true;
                }
            }

            if (done)
                _projectiles.Remove(projectile);
        }
    }

    private bool Strike(LiveHero owner, LiveProjectile projectile, LiveMonster victim, List<LiveEvent> events)
    {
        projectile.Struck.Add(victim.Id);

        if (projectile.Kind == ProjectileKind.Spell)
        {
            Land(owner, projectile, projectile.Position, victim, events);
            return true;
        }

        var travelled = projectile.Origin.DistanceTo(victim.Position);
        var pointBlank = travelled < LiveRules.PointBlankDistance;
        var stats = owner.Player.CombatStats;
        var farShot = owner.Player.EffectAmount(EffectKind.FarShotCritical);

        if (farShot > 0 && travelled >= Game.FarShotDistance)
            stats = stats.WithCritical(stats.Critical + farShot);

        var blow = new Blow(stats, projectile.Weapon, Glancing: pointBlank, Multiplier: projectile.Multiplier);

        if (projectile.PierceCount > 0)
            blow = blow with { Multiplier = 1, Stats = stats.WithAttack(stats.Attack * projectile.PiercePercent / 100) };

        HitMonster(owner, victim, blow, events);
        projectile.PierceCount++;
        return !projectile.Pierces;
    }

    private void Land(LiveHero owner, LiveProjectile projectile, Vector at, LiveMonster? victim, List<LiveEvent> events)
    {
        if (projectile.Spell is not { } spell)
            return;

        _spellPercent = projectile.PowerPercent;

        if (projectile.Size > 0)
            Explode(owner, spell, projectile.Rarity, at, projectile.Size + 0.5, events);
        else if (victim is not null)
        {
            SpellHit(owner, victim, spell, projectile.Rarity, events);

            if (spell.Shape == SpellShape.Chain)
                ChainFrom(owner, victim, spell, projectile.Rarity, Math.Max(1, spell.Size), events);
        }

        _spellPercent = 100;
    }

    private void Cast(LiveHero hero, SpellDefinition spell, Rarity rarity, List<LiveEvent> events, bool echo = false)
    {
        var player = hero.Player;

        if (!echo)
            player.SpendMana(player.SpellCost(spell));

        var overloaded = !echo && player.IsOverloaded;

        if (!echo)
            player.IsOverloaded = false;
        var size = spell.Size + (overloaded ? AbilityRules.OverloadAreaBonus : 0);
        hero.Touch();

        switch (spell.Shape)
        {
            case SpellShape.Bolt when size == 0:
                Fire(hero, ProjectileKind.Spell, LiveRules.ShotRange, events, spell: spell, rarity: rarity);
                break;
            case SpellShape.Bolt:
            case SpellShape.Burst:
                Fire(hero, ProjectileKind.Spell, LiveRules.ShotRange, events, spell: spell, rarity: rarity, size: size);
                break;
            case SpellShape.Chain:
                Fire(hero, ProjectileKind.Spell, LiveRules.ShotRange, events, spell: spell, rarity: rarity);
                break;
            case SpellShape.Line:
                Sweep(hero, spell, rarity, LiveRules.ShotRange + (overloaded ? AbilityRules.OverloadAreaBonus : 0), events);
                break;
            case SpellShape.Nova:
            case SpellShape.Storm:
                Explode(hero, spell, rarity, hero.Position, size + 0.5, events, sparesCaster: true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(spell), spell.Shape, "Unknown spell shape.");
        }
    }

    private void Explode(LiveHero hero, SpellDefinition spell, Rarity rarity, Vector center, double radius, List<LiveEvent> events, bool sparesCaster = false)
    {
        events.Add(new SpellUnleashed(hero.Id, spell, center, radius));

        foreach (var victim in _monsters.Where(monster => !monster.IsDead && monster.Position.DistanceTo(center) <= radius + monster.Radius).ToList())
        {
            if (sparesCaster || Collision.ClearPath(Map, center, victim.Position, 0.05))
                SpellHit(hero, victim, spell, rarity, events);
        }
    }

    private void Sweep(LiveHero hero, SpellDefinition spell, Rarity rarity, double length, List<LiveEvent> events)
    {
        var end = hero.Position;

        for (var travelled = 0.0; travelled < length; travelled += LiveRules.ProjectileStep)
        {
            var next = end + hero.Facing * LiveRules.ProjectileStep;

            if (Collision.HitsWall(Map, next, LiveRules.ProjectileRadius))
                break;

            end = next;
        }

        events.Add(new SpellUnleashed(hero.Id, spell, end, 0));
        var line = end - hero.Position;
        var span = line.Length;

        foreach (var victim in _monsters.Where(monster => !monster.IsDead).ToList())
        {
            var along = span < 1e-6 ? 0 : (victim.Position - hero.Position).Dot(line * (1 / span));

            if (along < 0 || along > span + victim.Radius)
                continue;

            var closest = hero.Position + line * (Math.Clamp(along, 0, span) / Math.Max(span, 1e-6));

            if (closest.DistanceTo(victim.Position) <= victim.Radius + LiveRules.LineHalfWidth)
                SpellHit(hero, victim, spell, rarity, events);
        }
    }

    private void ChainFrom(LiveHero hero, LiveMonster first, SpellDefinition spell, Rarity rarity, int jumps, List<LiveEvent> events)
    {
        var struck = new HashSet<int> { first.Id };
        var source = first;

        for (var jump = 0; jump < jumps; jump++)
        {
            var next = _monsters
                .Where(monster => !monster.IsDead && !struck.Contains(monster.Id) && monster.Position.DistanceTo(source.Position) <= Game.ChainJumpDistance)
                .OrderBy(monster => monster.Position.DistanceTo(source.Position))
                .FirstOrDefault();

            if (next is null)
                return;

            struck.Add(next.Id);
            SpellHit(hero, next, spell, rarity, events);
            source = next;
        }
    }

    private void SpellHit(LiveHero hero, LiveMonster target, SpellDefinition spell, Rarity rarity, List<LiveEvent> events)
    {
        if (target.IsDead)
            return;

        var player = hero.Player;
        var stats = player.CombatStats.WithAttack(player.CombatStats.Attack + spell.Power);
        var damage = Math.Max(DamageCalculator.MinimumDamage, DamageCalculator.BaseDamage(stats, target.Stats) * _spellPercent / 100);
        var critical = _random.NextDouble() < Math.Min(stats.Critical, DamageCalculator.MaxCritical) / 100.0;
        var rolled = new AttackResult(critical ? damage * DamageCalculator.CriticalMultiplier : damage, critical);
        var element = SpellElement(hero, spell, target);
        var result = ChargeInWater(ElementRules.Apply(rolled, element, target.Kind.Weakness, target.Kind.Resistance, rarity), element, target);

        Wound(hero, target, result, events);

        if (!target.IsDead)
            TriggerElement(hero, element, rarity, target, result.Damage, events, spell.AlwaysTriggers, player.EffectAmount(EffectKind.SpellMastery));
    }

    private void HitMonster(LiveHero hero, LiveMonster target, Blow blow, List<LiveEvent> events)
    {
        if (target.IsDead)
            return;

        blow = BeforeExoticHit(hero, target, blow, events);

        if (target.IsMarked(Time))
            blow = blow with { DamagePercent = blow.DamagePercent * (100 + Game.HuntressMarkBonusPercent) / 100 };

        var defender = blow.IgnoresDefense ? target.Stats.WithDefense(0) : target.Stats;
        var ambushMultiplier = DamageCalculator.CriticalMultiplier + (hero.Player.EffectAmount(EffectKind.Ambush) > 0 ? 1 : 0);
        var rolled = blow.Ambush
            ? new AttackResult(DamageCalculator.BaseDamage(blow.Stats, defender) * ambushMultiplier, IsCritical: true)
            : _damage.Calculate(blow.Stats, defender);

        if (!rolled.Missed && blow.Multiplier > 1)
            rolled = rolled with { Damage = rolled.Damage * blow.Multiplier };

        if (!rolled.Missed && (blow.Bonus > 0 || blow.DamagePercent != 100))
            rolled = rolled with { Damage = (rolled.Damage + blow.Bonus) * blow.DamagePercent / 100 };

        if (!rolled.Missed && blow.Glancing)
            rolled = rolled with { Damage = Math.Max(DamageCalculator.MinimumDamage, rolled.Damage / Game.GlancingBlowDivisor) };

        var weapon = blow.Weapon;
        var result = ChargeInWater(ElementRules.Apply(rolled, weapon?.Element, target.Kind.Weakness, target.Kind.Resistance, weapon?.Rarity ?? Rarity.Common), weapon?.Element, target);

        Wound(hero, target, result, events);
        DrainLife(hero, result, events);
        AfterExoticHit(hero, target, blow, result);

        if (!target.IsDead && !result.Missed && blow.TriggersElement && weapon?.Element is { } element)
            TriggerElement(hero, element, weapon.Rarity, target, result.Damage, events);
    }

    private void DrainLife(LiveHero hero, AttackResult result, List<LiveEvent> events)
    {
        var player = hero.Player;
        var percent = player.EffectAmount(EffectKind.LifeSteal) + (player.FuryTurns > 0 ? player.EffectAmount(EffectKind.FuryLifeSteal) : 0);

        if (percent <= 0 || result.Missed || result.Damage <= 0 || hero.Health >= hero.MaxHealth)
            return;

        var amount = Math.Max(1, result.Damage * percent / 100);
        player.Heal(amount);
        hero.Touch();
        events.Add(new HeroLifeDrained(hero.Id, amount));
    }

    private void Wound(LiveHero hero, LiveMonster target, AttackResult result, List<LiveEvent> events)
    {
        var wasMarked = target.IsMarked(Time);

        if (!result.Missed)
            target.TakeDamage(result.Damage);

        if (!target.IsAwake)
        {
            target.IsAwake = true;
            events.Add(new MonsterWoke(target.Id));
        }

        events.Add(new MonsterHit(hero.Id, target.Id, result, target.Health));

        if (!target.IsDead)
            return;

        events.Add(new MonsterSlain(hero.Id, target.Id));
        Reward(hero, target, events);
        AfterKill(hero, target, wasMarked, events);
    }

    private void TriggerElement(LiveHero hero, Element element, Rarity rarity, LiveMonster target, int damage, List<LiveEvent> events, bool guaranteed = false, int extraChance = 0)
    {
        var boost = hero.Player.EffectAmount(BoostFor(element));
        var chance = guaranteed ? 1.0 : ElementRules.EffectChance(element, rarity) + (boost + extraChance) / 100.0;

        if (chance < 1 && _random.NextDouble() >= chance)
            return;

        var boosted = boost > 0;

        switch (element)
        {
            case Element.Fire:
                target.BurnSecondsLeft = ElementRules.BurnTurns;
                target.BurnDamage = ElementRules.BurnDamage(rarity) + (boosted ? 1 : 0);
                target.BurnOwnerId = hero.Id;
                events.Add(new MonsterIgnited(target.Id));
                break;
            case Element.Ice when Time >= target.FreezeImmuneUntil && !IsFrozen(target):
                var seconds = target.Kind.IsBoss ? ElementRules.FreezeTurns : ElementRules.FreezeTurns + (boosted ? 1 : 0);
                target.FrozenUntil = Time + seconds;
                target.FreezeImmuneUntil = target.FrozenUntil + ElementRules.FreezeImmunityTurns;
                target.WindupEndsAt = null;
                events.Add(new MonsterFrozen(target.Id));
                break;
            case Element.Ice:
                break;
            case Element.Lightning:
                Shock(hero, target, damage, rarity, boosted ? 2 : 1, events);
                break;
            case Element.Wind when !target.Kind.IsBoss:
                var push = (target.Position - hero.Position).Normalized() * (boosted ? Game.WindBoostPush : 1);
                target.Position = Collision.Slide(Map, target.Position, push, target.Radius, passesThroughWalls: target.Kind.PassesThroughWalls);
                target.WindupEndsAt = null;
                events.Add(new MonsterPushed(target.Id));
                break;
            case Element.Wind:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown element.");
        }
    }

    private void Shock(LiveHero hero, LiveMonster source, int damage, Rarity rarity, int jumps, List<LiveEvent> events)
    {
        var struck = new HashSet<int> { source.Id };
        var from = source;

        for (var jump = 0; jump < jumps; jump++)
        {
            var victim = _monsters
                .Where(monster => !monster.IsDead && !struck.Contains(monster.Id) && monster.Position.DistanceTo(from.Position) <= LiveRules.ShockDistance)
                .OrderBy(monster => monster.Position.DistanceTo(from.Position))
                .FirstOrDefault();

            if (victim is null)
                return;

            struck.Add(victim.Id);
            var result = ChargeInWater(
                ElementRules.Apply(
                    new AttackResult(Math.Max(DamageCalculator.MinimumDamage, (damage + 1) / 2), IsCritical: false),
                    Element.Lightning,
                    victim.Kind.Weakness,
                    victim.Kind.Resistance,
                    rarity),
                Element.Lightning,
                victim);

            Wound(hero, victim, result, events);
            from = victim;
        }
    }

    private void BurnMonsters(List<LiveEvent> events)
    {
        foreach (var monster in _monsters.Where(monster => !monster.IsDead && monster.BurnSecondsLeft > 0).ToList())
        {
            monster.BurnSecondsLeft--;
            monster.TakeDamage(monster.BurnDamage);
            var owner = _heroes.FirstOrDefault(hero => hero.Id == monster.BurnOwnerId);
            events.Add(new MonsterScorched(monster.BurnOwnerId, monster.Id, monster.BurnDamage, monster.Health));

            if (!monster.IsDead)
                continue;

            events.Add(new MonsterSlain(monster.BurnOwnerId, monster.Id));

            if (owner is not null)
                Reward(owner, monster, events);
        }
    }

    private static EffectKind BoostFor(Element element) => element switch
    {
        Element.Fire => EffectKind.FireBoost,
        Element.Ice => EffectKind.IceBoost,
        Element.Lightning => EffectKind.LightningBoost,
        Element.Wind => EffectKind.WindBoost,
        _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown element.")
    };
}
