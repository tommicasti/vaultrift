using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public sealed partial class LiveFloor
{
    private int _spellPercent = 100;

    private Blow BeforeExoticHit(LiveHero hero, LiveMonster target, Blow blow, List<LiveEvent> events)
    {
        var player = hero.Player;

        if (blow.Weapon is null || !ReferenceEquals(blow.Weapon, player.Weapon))
            return blow;

        if (hero.Wields(ExoticPower.Riftrender))
        {
            hero.ComboCount = hero.ComboTargetId == target.Id ? hero.ComboCount + 1 : 1;
            hero.ComboTargetId = target.Id;

            if (hero.ComboCount % Game.RiftrenderEvery == 0)
            {
                events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.Riftrender, target.Id));
                blow = blow with { Multiplier = blow.Multiplier * 2, IgnoresDefense = true };
            }
        }

        if (hero.Wields(ExoticPower.Oathbound) && hero.Riposte > 0)
        {
            events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.Oathbound, target.Id, hero.Riposte));
            blow = blow with { Bonus = blow.Bonus + hero.Riposte };
            hero.Riposte = 0;
        }

        return blow;
    }

    private void AfterExoticHit(LiveHero hero, LiveMonster target, Blow blow, AttackResult result)
    {
        if (target.IsDead || result.Missed || blow.Weapon is null || !ReferenceEquals(blow.Weapon, hero.Player.Weapon))
            return;

        if (hero.Wields(ExoticPower.HuntressPromise))
            target.MarkedUntil = Time + LiveRules.MarkSeconds;

        if (hero.Wields(ExoticPower.WidowsKiss))
        {
            target.VenomStacks = Math.Min(Game.WidowMaxStacks, target.VenomStacks + 1);
            target.VenomOwnerId = hero.Id;
        }
    }

    private void AfterKill(LiveHero hero, LiveMonster target, bool wasMarked, List<LiveEvent> events)
    {
        var player = hero.Player;

        if (wasMarked)
        {
            var amount = Math.Max(1, hero.MaxHealth * Game.HuntressHealPercent / 100);
            player.Heal(amount);
            events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.HuntressPromise, target.Id, amount));
        }

        if (hero.Wields(ExoticPower.HeadsmansMoon) && Time < hero.AttackReadyAt)
        {
            hero.AttackReadyAt = Time;
            events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.HeadsmansMoon, target.Id));
        }
    }

    private void Stagger(LiveHero hero, IEnumerable<LiveMonster> struck, List<LiveEvent> events)
    {
        if (!hero.Wields(ExoticPower.Worldsplitter))
            return;

        foreach (var monster in struck.Where(monster => !monster.IsDead && !monster.Kind.IsBoss))
        {
            monster.StaggeredUntil = Time + LiveRules.StaggerSeconds;
            monster.WindupEndsAt = null;
            events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.Worldsplitter, monster.Id));
        }
    }

    private bool TryShadowstep(LiveHero hero, AttackProfile profile, List<LiveEvent> events)
    {
        if (!hero.Wields(ExoticPower.Shadowstep) || Time < hero.ShadowstepReadyAt)
            return false;

        var target = _monsters
            .Where(monster => !monster.IsDead)
            .Where(monster =>
            {
                var offset = monster.Position - hero.Position;
                var distance = offset.Length;
                return distance > profile.Reach + monster.Radius &&
                       distance <= profile.Reach + monster.Radius + LiveRules.ShadowstepExtraReach &&
                       hero.Facing.Dot(offset * (1 / distance)) >= 0.7;
            })
            .Where(monster => Collision.ClearPath(Map, hero.Position, monster.Position, LiveRules.HeroRadius))
            .OrderBy(monster => monster.Position.DistanceTo(hero.Position))
            .FirstOrDefault();

        if (target is null)
            return false;

        var offset = target.Position - hero.Position;
        var distance = offset.Length;
        var landing = distance - target.Radius - LiveRules.HeroRadius - 0.05;
        var from = hero.Position;
        hero.Position = Collision.Slide(Map, from, offset * (landing / distance), LiveRules.HeroRadius);

        if (Map.IsLava(Collision.TileOf(hero.Position)))
        {
            hero.Position = from;
            return false;
        }

        hero.Facing = offset * (1 / distance);
        hero.ShadowstepReadyAt = Time + LiveRules.ShadowstepCooldownSeconds;
        hero.Touch();
        events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.Shadowstep, target.Id));
        HitMonster(hero, target, new Blow(hero.Player.CombatStats, hero.Player.Weapon, Ambush: true), events);
        return true;
    }

    private void GuardTheWall(LiveMonster monster, LiveHero hero, List<LiveEvent> events)
    {
        if (!hero.Wields(ExoticPower.LastWall) || hero.IsDead || Time - monster.LastWallStruckAt < LiveRules.LastWallCooldownSeconds)
            return;

        var profile = AttackProfile.For(hero.Player);

        if (!InArc(hero, monster, profile with { Reach = Math.Max(profile.Reach, LiveRules.HeroReach) }))
            return;

        monster.LastWallStruckAt = Time;
        events.Add(new LiveExoticTriggered(hero.Id, ExoticPower.LastWall, monster.Id));
        HitMonster(hero, monster, new Blow(hero.Player.CombatStats, hero.Player.Weapon), events);
    }

    private void SwearRevenge(LiveHero hero, AttackResult result)
    {
        if (hero.Wields(ExoticPower.Oathbound) && !result.Missed && result.Damage > 0)
            hero.Riposte += result.Damage;
    }

    private void SpreadVenom(List<LiveEvent> events)
    {
        foreach (var monster in _monsters.Where(monster => !monster.IsDead && monster.VenomStacks > 0).ToList())
        {
            var damage = monster.VenomStacks * Game.WidowDamagePerStack;
            monster.TakeDamage(damage);
            events.Add(new MonsterEnvenomed(monster.VenomOwnerId, monster.Id, damage, monster.VenomStacks, monster.Health));

            if (!monster.IsDead)
                continue;

            events.Add(new MonsterSlain(monster.VenomOwnerId, monster.Id));

            if (_heroes.FirstOrDefault(hero => hero.Id == monster.VenomOwnerId) is { } owner)
                Reward(owner, monster, events);
        }
    }

    private static AttackProfile ExoticReach(LiveHero hero, AttackProfile profile) =>
        hero.Wields(ExoticPower.StarfallLance) && profile.Style == AttackStyle.Thrust
            ? profile with { Reach = profile.Reach + LiveRules.StarfallExtraReach }
            : profile;

    private static Element SpellElement(LiveHero hero, SpellDefinition spell, LiveMonster target) =>
        hero.Wields(ExoticPower.Prism) && target.Kind.Weakness is { } weakness ? weakness : spell.Element;
}
