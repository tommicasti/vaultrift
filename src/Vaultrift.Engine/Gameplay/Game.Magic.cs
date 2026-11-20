using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int SpellRange = BowRange;
    public const int ChainJumpDistance = 3;

    public TurnResult SelectSpell(int index)
    {
        GameEvent outcome = Player.SelectSpell(index)
            ? new SpellSelected(index)
            : new SpellRejected(SpellResult.UnknownSpell);

        return new TurnResult(TurnPassed: false, [outcome]);
    }

    public TurnResult LearnSpell(int entryId, int key)
    {
        var entry = Player.Inventory.Find(entryId);

        if (entry is null)
            return SpellRejected(SpellResult.ItemNotFound);

        if (entry.Item.Definition is not ScrollDefinition scroll)
            return SpellRejected(SpellResult.NotAScroll);

        var result = Player.CanLearn(scroll, key);

        if (result != SpellResult.Done)
            return SpellRejected(result);

        var replaced = Player.ScrollOnKey(key);

        if (replaced is not null && entry.Quantity > 1 && !Player.Inventory.CanAdd(new ItemInstance(replaced)))
            return SpellRejected(SpellResult.BackpackFull);

        Player.Inventory.RemoveOne(entryId);
        Player.Learn(scroll, key);

        if (replaced is not null)
            Player.Inventory.TryAdd(new ItemInstance(replaced));

        return new TurnResult(TurnPassed: false, [new SpellLearned(scroll.Spell, key, replaced?.Spell)]);
    }

    private static TurnResult SpellRejected(SpellResult reason) =>
        new(TurnPassed: false, [new SpellRejected(reason)]);

    private TurnResult ReadScroll(InventoryEntry entry, ScrollDefinition scroll)
    {
        if (Player.Mana < Player.SpellCost(scroll.Spell))
            return new TurnResult(TurnPassed: false, [new UseRejected(UseResult.NotEnoughMana)]);

        var events = new List<GameEvent> { new ItemUsed(entry.Item) };
        Player.Inventory.RemoveOne(entry.Id);
        BeginAmbush();
        Cast(scroll.Spell, scroll.Rarity, events);
        EndAmbush(events);
        EndTurn(events);
        return new TurnResult(TurnPassed: true, events);
    }

    private void CastFromStaff(ItemInstance staff, List<GameEvent> events)
    {
        var spell = Player.ActiveSpell!;

        if (Player.Mana < Player.SpellCost(spell))
        {
            events.Add(new SpellRejected(SpellResult.NotEnoughMana));
            Strike(staff, Player.CombatStats, new HitOptions(Glancing: true, TriggersElement: false), events);
            return;
        }

        Cast(spell, staff.Rarity, events);

        if (!Wields(Items.ExoticPower.Echo))
            return;

        events.Add(new ExoticTriggered(Items.ExoticPower.Echo));
        _spellPowerPercent = EchoPowerPercent;
        Cast(spell, staff.Rarity, events, echo: true);
        _spellPowerPercent = 100;
    }

    private void Cast(SpellDefinition spell, Rarity rarity, List<GameEvent> events, bool echo = false)
    {
        if (!echo)
            Player.SpendMana(Player.SpellCost(spell));

        var overloaded = !echo && Player.IsOverloaded;

        if (!echo)
            Player.IsOverloaded = false;

        var size = spell.Size + (overloaded ? AbilityRules.OverloadAreaBonus : 0);

        switch (spell.Shape)
        {
            case SpellShape.Bolt when size == 0:
                CastBolt(spell, rarity, events);
                break;
            case SpellShape.Bolt:
            case SpellShape.Burst:
                CastBurst(spell, rarity, size, events);
                break;
            case SpellShape.Line:
                CastLine(spell, rarity, overloaded, events);
                break;
            case SpellShape.Chain:
                CastChain(spell, rarity, size, events);
                break;
            case SpellShape.Nova:
            case SpellShape.Storm:
                CastAround(spell, rarity, size, events);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(spell), spell.Shape, "Unknown spell shape.");
        }
    }

    private void CastBolt(SpellDefinition spell, Rarity rarity, List<GameEvent> events)
    {
        var (path, target, _) = Fly();
        events.Add(new SpellCast(spell, path, target is null ? [] : [target.Position]));

        if (target is null)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        SpellHit(target, spell, rarity, events);
    }

    private void CastBurst(SpellDefinition spell, Rarity rarity, int radius, List<GameEvent> events)
    {
        var (path, _, impact) = Fly();
        var area = Area(impact, radius);
        events.Add(new SpellCast(spell, path, area));

        var victims = EnemiesIn(area);

        if (victims.Count == 0)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        foreach (var victim in victims.Where(enemy => !enemy.IsDead))
        {
            SpellHit(victim, spell, rarity, events, triggersElement: spell.Element != Element.Wind);

            if (spell.Element == Element.Wind && !victim.IsDead)
                Pull(victim, impact, events);
        }
    }

    private void CastLine(SpellDefinition spell, Rarity rarity, bool overloaded, List<GameEvent> events)
    {
        var path = new List<Position>();
        var victims = new List<Enemy>();
        var position = Player.Position;

        for (var step = 1; step <= SpellRange + (overloaded ? AbilityRules.OverloadAreaBonus : 0); step++)
        {
            position = position.Move(Player.Facing);
            var target = FindLivingEnemyAt(position);

            if (target is null && (!Map.IsWalkable(position) || IsChest(position)))
                break;

            path.Add(position);

            if (target is not null)
                victims.Add(target);
        }

        events.Add(new SpellCast(spell, path, path));

        if (victims.Count == 0)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        foreach (var victim in Enumerable.Reverse(victims).Where(enemy => !enemy.IsDead))
        {
            var damage = SpellHit(victim, spell, rarity, events, triggersElement: false);

            if (!victim.IsDead && damage > 0)
                KnockBack(victim, damage, spell.Size, events);
        }
    }

    private void CastChain(SpellDefinition spell, Rarity rarity, int targets, List<GameEvent> events)
    {
        var (path, first, _) = Fly();
        var struck = new List<Enemy>();
        var current = first;

        while (current is not null && struck.Count < Math.Max(1, targets))
        {
            struck.Add(current);
            var from = current.Position;
            current = _enemies
                .Where(enemy => !enemy.IsDead && !struck.Contains(enemy) && Distance(enemy.Position, from) <= ChainJumpDistance)
                .OrderBy(enemy => Distance(enemy.Position, from))
                .ThenBy(enemy => enemy.Id)
                .FirstOrDefault();
        }

        events.Add(new SpellCast(spell, path, struck.Select(enemy => enemy.Position).ToList()));

        if (struck.Count == 0)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        foreach (var victim in struck.Where(enemy => !enemy.IsDead))
            SpellHit(victim, spell, rarity, events);
    }

    private void CastAround(SpellDefinition spell, Rarity rarity, int radius, List<GameEvent> events)
    {
        var area = Area(Player.Position, radius).Where(tile => tile != Player.Position).ToList();
        events.Add(new SpellCast(spell, [], area));

        var victims = EnemiesIn(area);

        if (victims.Count == 0)
        {
            events.Add(new PlayerAttackedNothing());
            return;
        }

        foreach (var victim in victims.Where(enemy => !enemy.IsDead))
            SpellHit(victim, spell, rarity, events);
    }

    private int SpellHit(Enemy target, SpellDefinition spell, Rarity rarity, List<GameEvent> events, bool triggersElement = true)
    {
        var stats = Player.CombatStats.WithAttack(Player.CombatStats.Attack + spell.Power);
        var rolled = RollSpell(stats, target);

        if (!rolled.Missed && _spellPowerPercent != 100)
            rolled = rolled with { Damage = Math.Max(DamageCalculator.MinimumDamage, rolled.Damage * _spellPowerPercent / 100) };

        var result = ChargeInWater(ElementRules.Apply(
            rolled,
            SpellElementAgainst(target, spell),
            target.Kind.Weakness,
            target.Kind.Resistance,
            rarity), target);

        target.TakeDamage(result.Damage);
        events.Add(new PlayerAttackedEnemy(target.Id, result));
        DrainLife(result.Damage, events);

        if (target.IsDead)
        {
            if (_ambushStruck)
                _ambushKilled = true;

            Defeat(target, events);
            return result.Damage;
        }

        WakeUp(target, events);

        if (triggersElement)
        {
            TriggerElement(spell.Element, rarity, target, result.Damage, events, spell.AlwaysTriggers,
                Player.EffectAmount(EffectKind.SpellMastery));
        }

        return result.Damage;
    }

    private AttackResult RollSpell(Stats stats, Enemy target)
    {
        if (_ambushReady)
            return RollHit(stats, target);

        var damage = DamageCalculator.BaseDamage(stats, target.CombatStats);
        var critical = _random.NextDouble() < Math.Min(stats.Critical, DamageCalculator.MaxCritical) / 100.0;

        return new AttackResult(critical ? damage * DamageCalculator.CriticalMultiplier : damage, critical);
    }

    private (List<Position> Path, Enemy? Target, Position Impact) Fly()
    {
        var path = new List<Position>();
        var position = Player.Position;

        for (var step = 1; step <= SpellRange; step++)
        {
            var next = position.Move(Player.Facing);
            var target = FindLivingEnemyAt(next);

            if (target is not null)
            {
                path.Add(next);
                return (path, target, next);
            }

            if (!Map.IsWalkable(next) || IsChest(next))
                return (path, null, path.Count > 0 ? position : next);

            path.Add(next);
            position = next;
        }

        return (path, null, position);
    }

    private void Pull(Enemy enemy, Position center, List<GameEvent> events)
    {
        if (enemy.Position == center || enemy.Kind.IsStationary || enemy.Kind.IsBoss)
            return;

        foreach (var direction in ApproachDirections(enemy.Position, center))
        {
            var target = enemy.Position.Move(direction);

            if (!CanBePushedInto(enemy, target))
                continue;

            enemy.Position = target;
            events.Add(new EnemyPulled(enemy.Id, target));
            return;
        }
    }

    private List<Position> Area(Position center, int radius)
    {
        var tiles = new List<Position>();

        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                var tile = new Position(center.X + dx, center.Y + dy);

                if (Map.IsInside(tile))
                    tiles.Add(tile);
            }
        }

        return tiles;
    }

    private List<Enemy> EnemiesIn(IReadOnlyCollection<Position> area) =>
        _enemies.Where(enemy => !enemy.IsDead && area.Contains(enemy.Position)).ToList();

    private void SiphonMana(List<GameEvent> events)
    {
        var amount = Math.Min(Player.EffectAmount(EffectKind.ManaSiphon), Player.MaxMana - Player.Mana);

        if (amount <= 0)
            return;

        Player.RestoreMana(amount);
        events.Add(new ManaSiphoned(amount));
    }
}
