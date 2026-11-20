using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int RiftrenderEvery = 3;
    public const int HuntressMarkTurns = 3;
    public const int HuntressMarkBonusPercent = 50;
    public const int HuntressHealPercent = 10;
    public const int WidowMaxStacks = 5;
    public const int WidowDamagePerStack = 2;
    public const int ShadowstepCooldown = 3;
    public const int RiftNeedlePiercePercent = 75;
    public const int EchoPowerPercent = 50;

    private int _comboTarget = -1;
    private int _comboCount;
    private int _riposte;
    private int _shadowstepCooldown;
    private int _spellPowerPercent = 100;

    public ExoticPower? ExoticPower => (Player.Weapon?.Definition as WeaponDefinition)?.Exotic;

    public int ShadowstepCooldownLeft => _shadowstepCooldown;

    private bool Wields(ExoticPower power) => ExoticPower == power;

    private HitOptions BeforeExoticHit(Enemy target, ItemInstance? weapon, HitOptions options, List<GameEvent> events)
    {
        if (weapon is null || !ReferenceEquals(weapon, Player.Weapon))
            return options;

        if (Wields(Items.ExoticPower.Riftrender))
        {
            if (_comboTarget == target.Id)
            {
                _comboCount++;
            }
            else
            {
                _comboTarget = target.Id;
                _comboCount = 1;
            }

            if (_comboCount % RiftrenderEvery == 0)
            {
                events.Add(new ExoticTriggered(Items.ExoticPower.Riftrender, target.Id));
                options = options with { DamageMultiplier = options.DamageMultiplier * 2, IgnoresDefense = true };
            }
        }

        if (Wields(Items.ExoticPower.Oathbound) && _riposte > 0)
        {
            events.Add(new ExoticTriggered(Items.ExoticPower.Oathbound, target.Id, _riposte));
            options = options with { BonusDamage = options.BonusDamage + _riposte };
            _riposte = 0;
        }

        if (target.MarkedTurns > 0)
            options = options with { DamagePercent = options.DamagePercent * (100 + HuntressMarkBonusPercent) / 100 };

        return options;
    }

    private void AfterExoticHit(Enemy target, ItemInstance? weapon, AttackResult result, bool wasMarked, List<GameEvent> events)
    {
        if (target.IsDead)
        {
            if (wasMarked)
                HuntressHeal(target, events);

            return;
        }

        if (weapon is null || !ReferenceEquals(weapon, Player.Weapon) || result.Missed)
            return;

        if (Wields(Items.ExoticPower.HuntressPromise))
            target.MarkedTurns = HuntressMarkTurns;

        if (Wields(Items.ExoticPower.WidowsKiss))
            target.VenomStacks = Math.Min(WidowMaxStacks, target.VenomStacks + 1);
    }

    private void HuntressHeal(Enemy target, List<GameEvent> events)
    {
        var amount = Math.Max(1, Player.Stats.MaxHealth * HuntressHealPercent / 100);
        Player.Heal(amount);
        events.Add(new ExoticTriggered(Items.ExoticPower.HuntressPromise, target.Id, amount));
    }

    private void Stagger(IEnumerable<Enemy> struck, List<GameEvent> events)
    {
        if (!Wields(Items.ExoticPower.Worldsplitter))
            return;

        foreach (var enemy in struck.Where(enemy => !enemy.IsDead && !enemy.Kind.IsBoss))
        {
            enemy.StaggeredTurns = 1;
            events.Add(new ExoticTriggered(Items.ExoticPower.Worldsplitter, enemy.Id));
        }
    }

    private bool TryShadowstep(ItemInstance weapon, List<GameEvent> events)
    {
        if (!Wields(Items.ExoticPower.Shadowstep) || _shadowstepCooldown > 0)
            return false;

        var landing = Player.Position.Move(Player.Facing);

        if (FindLivingEnemyAt(landing) is not null || !IsFree(landing) || Map.IsLava(landing))
            return false;

        if (FindLivingEnemyAt(landing.Move(Player.Facing)) is not { } target)
            return false;

        Player.Position = landing;
        events.Add(new PlayerMoved(landing));
        RecoverGraveAt(landing, events);
        events.Add(new ExoticTriggered(Items.ExoticPower.Shadowstep, target.Id));
        _shadowstepCooldown = ShadowstepCooldown;
        Hit(target, weapon, Player.CombatStats, new HitOptions(GuaranteedCritical: true), events);
        return true;
    }

    private void GuardTheWall(Enemy enemy, List<GameEvent> events)
    {
        if (!Wields(Items.ExoticPower.LastWall) || enemy.IsDead || enemy.Position != Player.Position.Move(Player.Facing))
            return;

        events.Add(new ExoticTriggered(Items.ExoticPower.LastWall, enemy.Id));
        Hit(enemy, Player.Weapon, Player.CombatStats, new HitOptions(), events);
    }

    private void SwearRevenge(AttackResult result)
    {
        if (Wields(Items.ExoticPower.Oathbound) && !result.Missed && result.Damage > 0)
            _riposte += result.Damage;
    }

    private void TickExotics(List<GameEvent> events)
    {
        if (_shadowstepCooldown > 0)
            _shadowstepCooldown--;

        foreach (var enemy in _enemies.Where(enemy => !enemy.IsDead))
        {
            if (enemy.MarkedTurns > 0)
                enemy.MarkedTurns--;

            if (enemy.VenomStacks == 0)
                continue;

            var damage = enemy.VenomStacks * WidowDamagePerStack;
            enemy.TakeDamage(damage);
            events.Add(new EnemyEnvenomed(enemy.Id, damage, enemy.VenomStacks));

            if (enemy.IsDead)
            {
                enemy.ClearStatuses();
                Defeat(enemy, events);
            }
        }
    }

    private Element SpellElementAgainst(Enemy target, SpellDefinition spell) =>
        Wields(Items.ExoticPower.Prism) && target.Kind.Weakness is { } weakness ? weakness : spell.Element;
}
