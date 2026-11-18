using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int BowRange = 6;
    public const int GlancingBlowDivisor = 2;

    private void Strike(ItemInstance? weapon, List<GameEvent> events)
    {
        var target = FindLivingEnemyAt(Player.Position.Move(Player.Facing));

        if (target is null)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        Hit(target, weapon, glancing: false, triggersElement: true, events);
    }

    private void Sweep(ItemInstance weapon, List<GameEvent> events)
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

        if (target is not null)
            Hit(target, weapon, glancing: false, triggersElement: true, events);

        foreach (var bystander in bystanders.Where(enemy => !enemy.IsDead))
            Hit(bystander, weapon, glancing: true, triggersElement: false, events);
    }

    private void Shoot(ItemInstance weapon, List<GameEvent> events)
    {
        var path = new List<Position>();
        Enemy? target = null;
        var position = Player.Position;

        for (var step = 0; step < BowRange; step++)
        {
            position = position.Move(Player.Facing);

            if (!Map.IsWalkable(position))
                break;

            path.Add(position);
            target = FindLivingEnemyAt(position);

            if (target is not null)
                break;
        }

        events.Add(new ArrowFlew(path));

        if (target is null)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        Hit(target, weapon, glancing: path.Count == 1, triggersElement: true, events);
    }

    private void Hit(Enemy target, ItemInstance? weapon, bool glancing, bool triggersElement, List<GameEvent> events)
    {
        var rolled = _damageCalculator.Calculate(Player.CombatStats, target.CombatStats);

        if (glancing && !rolled.IsDodged)
            rolled = rolled with { Damage = Math.Max(DamageCalculator.MinimumDamage, rolled.Damage / GlancingBlowDivisor) };

        var result = ElementRules.Apply(
            rolled,
            weapon?.Element,
            target.Kind.Weakness,
            target.Kind.Resistance,
            weapon?.Rarity ?? Rarity.Common);

        target.TakeDamage(result.Damage);
        events.Add(new PlayerAttackedEnemy(target.Id, result));

        if (target.IsDead)
            events.Add(new EnemyDefeated(target.Id));
        else
            WakeUp(target, events);

        if (triggersElement && !result.IsDodged && weapon?.Element is { } element)
            TriggerElement(element, weapon.Rarity, target, result.Damage, events);
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
}