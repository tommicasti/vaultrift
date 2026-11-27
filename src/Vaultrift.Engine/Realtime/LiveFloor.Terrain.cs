using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed partial class LiveFloor
{
    private void DriftOnCurrents()
    {
        var drift = LiveRules.CurrentSpeed * LiveRules.TickSeconds;

        foreach (var hero in _heroes.Where(hero => !hero.IsDead))
        {
            if (Map.CurrentAt(Collision.TileOf(hero.Position)) is not { } flow)
                continue;

            var from = hero.Position;
            hero.Position = Collision.Slide(Map, from, Offset(flow) * drift, LiveRules.HeroRadius, point => BlockedByMonster(from, point) || BlockedByFixture(from, point));
            hero.IsMoving |= hero.Position != from;
        }

        foreach (var monster in _monsters.Where(monster => !monster.IsDead && monster.Kind.RidesCurrents))
        {
            if (Map.CurrentAt(Collision.TileOf(monster.Position)) is not { } flow)
                continue;

            MoveMonster(monster, Offset(flow) * drift);
        }
    }

    private static Vector Offset(Direction direction) => direction switch
    {
        Direction.Up => new Vector(0, -1),
        Direction.Down => new Vector(0, 1),
        Direction.Left => new Vector(-1, 0),
        _ => new Vector(1, 0)
    };

    private void BreatheSpores(List<LiveEvent> events)
    {
        foreach (var hero in _heroes.Where(hero => !hero.IsDead).ToList())
        {
            var player = hero.Player;

            if (Map.IsSpores(Collision.TileOf(hero.Position)))
            {
                if (!player.IsPoisoned)
                    events.Add(new HeroPoisoned(hero.Id));

                player.PoisonTurns = Game.PoisonTurns;
                hero.Touch();
            }

            if (!player.IsPoisoned)
                continue;

            var damage = Game.PoisonDamage(hero.MaxHealth);
            player.PoisonTurns--;
            events.Add(new HeroPoisonDamaged(hero.Id, damage));
            Hurt(hero, damage, events);
        }
    }

    private void BurnMonstersOnLava(List<LiveEvent> events)
    {
        foreach (var monster in _monsters.Where(monster => !monster.IsDead && !monster.Kind.IsFireproof && Map.IsLava(Collision.TileOf(monster.Position))).ToList())
        {
            var damage = Game.LavaDamage(monster.MaxHealth);
            monster.TakeDamage(damage);
            events.Add(new MonsterBurnedByLava(monster.Id, damage, monster.Health));

            if (monster.IsDead)
                events.Add(new MonsterSlain(0, monster.Id));
        }
    }

    private bool StepsIntoLava(LiveMonster monster, Vector next) =>
        !monster.Kind.IsFireproof &&
        Map.IsLava(Collision.TileOf(next)) &&
        !Map.IsLava(Collision.TileOf(monster.Position));

    private AttackResult ChargeInWater(AttackResult result, Element? element, LiveMonster target)
    {
        if (result.Missed || element != Element.Lightning || !Map.IsWater(Collision.TileOf(target.Position)))
            return result;

        return result with { Damage = result.Damage * Game.WaterLightningMultiplier, Effectiveness = Effectiveness.Effective };
    }

    private bool TryBreakTerrain(LiveHero hero, List<LiveEvent> events)
    {
        var front = Collision.TileOf(hero.Position + hero.Facing * LiveRules.SmashReach);

        if (front == Collision.TileOf(hero.Position))
            return false;

        if (Map.IsCrystal(front))
        {
            Shatter(hero, front, events);
            return true;
        }

        if (Map.GetTile(front) != TileType.Obstacle)
            return false;

        ChangeTiles([front], TileType.Floor, null, events);
        events.Add(new ObstacleBroken(hero.Id, front));
        return true;
    }

    private void Shatter(LiveHero hero, Position crystal, List<LiveEvent> events)
    {
        ChangeTiles([crystal], TileType.Floor, null, events);
        events.Add(new CrystalBroken(hero.Id, crystal));
        var center = new Vector(crystal.X, crystal.Y);

        foreach (var monster in _monsters.Where(monster => !monster.IsDead && monster.Position.DistanceTo(center) <= LiveRules.ShardRadius).ToList())
        {
            var damage = DamageCalculator.BaseDamage(hero.Player.CombatStats, monster.Stats);
            monster.TakeDamage(damage);
            events.Add(new MonsterShardHit(hero.Id, monster.Id, damage, monster.Health));

            if (!monster.IsDead)
                continue;

            events.Add(new MonsterSlain(hero.Id, monster.Id));
            Reward(hero, monster, events);
        }
    }
}
