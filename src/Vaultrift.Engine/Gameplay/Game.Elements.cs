using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    private static readonly Direction[] ChainDirections = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

    private void TriggerElement(Element element, Rarity rarity, Enemy target, int damage, List<GameEvent> events)
    {
        if (!Rolls(ElementRules.EffectChance(element, rarity)))
            return;

        switch (element)
        {
            case Element.Fire:
                Ignite(target, rarity, events);
                break;
            case Element.Ice:
                Freeze(target, events);
                break;
            case Element.Lightning:
                ChainLightning(target, damage, rarity, events);
                break;
            case Element.Wind:
                KnockBack(target, events);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown element.");
        }
    }

    private bool Rolls(double chance) => chance >= 1 || _random.NextDouble() < chance;

    private static void Ignite(Enemy target, Rarity rarity, List<GameEvent> events)
    {
        if (target.IsDead)
            return;

        target.BurnTurnsLeft = ElementRules.BurnTurns;
        target.BurnDamage = ElementRules.BurnDamage(rarity);
        events.Add(new EnemyIgnited(target.Id));
    }

    private static void Freeze(Enemy target, List<GameEvent> events)
    {
        if (target.IsDead || target.IsFrozen || target.FreezeImmunityTurnsLeft > 0)
            return;

        target.FrozenTurnsLeft = ElementRules.FreezeTurns;
        events.Add(new EnemyFrozen(target.Id));
    }

    private void ChainLightning(Enemy target, int damage, Rarity rarity, List<GameEvent> events)
    {
        var victim = ChainDirections
            .Select(direction => FindLivingEnemyAt(target.Position.Move(direction)))
            .FirstOrDefault(enemy => enemy is not null && enemy != target);

        if (victim is null)
            return;

        var result = ElementRules.Apply(
            new AttackResult(Math.Max(DamageCalculator.MinimumDamage, (damage + 1) / 2), IsCritical: false),
            Element.Lightning,
            victim.Kind.Weakness,
            victim.Kind.Resistance,
            rarity);

        victim.TakeDamage(result.Damage);
        events.Add(new EnemyShocked(victim.Id, result));

        if (victim.IsDead)
            events.Add(new EnemyDefeated(victim.Id));
    }

    private void KnockBack(Enemy target, List<GameEvent> events)
    {
        if (target.IsDead)
            return;

        var destination = target.Position.Move(Player.Facing);

        if (!CanEnemyEnter(target, destination))
            return;

        target.Position = destination;
        events.Add(new EnemyKnockedBack(target.Id, destination));
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
            enemy.TakeDamage(enemy.BurnDamage);
            enemy.BurnTurnsLeft--;
            events.Add(new EnemyBurned(enemy.Id, enemy.BurnDamage));

            if (enemy.IsDead)
            {
                enemy.ClearStatuses();
                events.Add(new EnemyDefeated(enemy.Id));
            }
        }
    }
}