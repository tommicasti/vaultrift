using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int WindBoostPush = 2;

    private static readonly Direction[] ChainDirections = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

    private void TriggerElement(Element element, Rarity rarity, Enemy target, int damage, List<GameEvent> events, bool guaranteed = false)
    {
        var boost = Player.EffectAmount(BoostFor(element));
        var chance = guaranteed ? 1.0 : ElementRules.EffectChance(element, rarity) + boost / 100.0;

        if (!Rolls(chance))
            return;

        var boosted = boost > 0;

        switch (element)
        {
            case Element.Fire:
                Ignite(target, ElementRules.BurnDamage(rarity) + (boosted ? 1 : 0), events);
                break;
            case Element.Ice:
                Freeze(target, ElementRules.FreezeTurns + (boosted ? 1 : 0), events);
                break;
            case Element.Lightning:
                ChainLightning(target, damage, rarity, boosted ? 2 : 1, events);
                break;
            case Element.Wind:
                KnockBack(target, damage, boosted ? WindBoostPush : 1, events);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown element.");
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

    private bool Rolls(double chance) => chance >= 1 || _random.NextDouble() < chance;

    private void Ignite(Enemy target, int burnDamage, List<GameEvent> events)
    {
        if (target.IsDead || Map.IsWater(target.Position))
            return;

        target.BurnTurnsLeft = ElementRules.BurnTurns;
        target.BurnDamage = burnDamage;
        events.Add(new EnemyIgnited(target.Id));
    }

    private static void Freeze(Enemy target, int turns, List<GameEvent> events)
    {
        if (target.IsDead || target.IsFrozen || target.FreezeImmunityTurnsLeft > 0)
            return;

        target.FrozenTurnsLeft = target.Kind.IsBoss ? ElementRules.FreezeTurns : turns;
        events.Add(new EnemyFrozen(target.Id));
    }

    private void ChainLightning(Enemy target, int damage, Rarity rarity, int jumps, List<GameEvent> events)
    {
        var struck = new HashSet<Enemy> { target };
        var source = target;

        for (var jump = 0; jump < jumps; jump++)
        {
            var victim = ChainDirections
                .Select(direction => FindLivingEnemyAt(source.Position.Move(direction)))
                .FirstOrDefault(enemy => enemy is not null && !struck.Contains(enemy));

            if (victim is null)
                return;

            struck.Add(victim);

            var result = ChargeInWater(ElementRules.Apply(
                new AttackResult(Math.Max(DamageCalculator.MinimumDamage, (damage + 1) / 2), IsCritical: false),
                Element.Lightning,
                victim.Kind.Weakness,
                victim.Kind.Resistance,
                rarity), victim);

            victim.TakeDamage(result.Damage);
            events.Add(new EnemyShocked(victim.Id, result));

            if (victim.IsDead)
                Defeat(victim, events);

            source = victim;
        }
    }

    private void KnockBack(Enemy target, int damage, int distance, List<GameEvent> events)
    {
        for (var step = 0; step < distance; step++)
        {
            if (target.IsDead)
                return;

            var destination = target.Position.Move(Player.Facing);

            if (!CanBePushedInto(target, destination))
            {
                Slam(target, damage, events);
                return;
            }

            target.Position = destination;
            events.Add(new EnemyKnockedBack(target.Id, destination));

            if (AvoidsLava(target, destination))
            {
                Scorch(target, events);
                return;
            }
        }
    }

    private void Slam(Enemy target, int damage, List<GameEvent> events)
    {
        var impact = Math.Max(DamageCalculator.MinimumDamage, (damage + 1) / 2);

        target.TakeDamage(impact);
        events.Add(new EnemySlammed(target.Id, impact));

        if (target.IsDead)
            Defeat(target, events);
    }

    private static void ThawOneTurn(Enemy enemy, List<GameEvent> events)
    {
        enemy.FrozenTurnsLeft--;

        if (enemy.IsFrozen)
            return;

        enemy.FreezeImmunityTurnsLeft = ElementRules.FreezeImmunityTurns;
        events.Add(new EnemyThawed(enemy.Id));
    }

    private void BurnEnemies(List<GameEvent> events)
    {
        foreach (var enemy in _enemies.Where(enemy => !enemy.IsDead && enemy.IsBurning))
        {
            if (Map.IsWater(enemy.Position))
            {
                enemy.BurnTurnsLeft = 0;
                continue;
            }

            enemy.TakeDamage(enemy.BurnDamage);
            enemy.BurnTurnsLeft--;
            events.Add(new EnemyBurned(enemy.Id, enemy.BurnDamage));

            if (enemy.IsDead)
            {
                enemy.ClearStatuses();
                Defeat(enemy, events);
            }
        }
    }
}
