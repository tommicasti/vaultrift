using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Enemy : Creature
{
    public Enemy(int id, Position position, Stats stats)
        : this(id, position, new EnemyKind("generic", "Enemy", stats))
    {
    }

    public Enemy(int id, Position position, EnemyKind kind, Rarity rarity = Rarity.Common, int level = 1)
        : base(position, ScaledStats(kind, rarity, level))
    {
        Id = id;
        StartPosition = position;
        Kind = kind;
        Rarity = rarity;
        Level = level;
    }

    public int Id { get; }

    public Position StartPosition { get; }

    public EnemyKind Kind { get; }

    public Rarity Rarity { get; }

    public int Level { get; }

    public bool IsAwake { get; internal set; }

    public bool IsResting { get; internal set; }

    public int StepsPerTurn => Speed == Speed.Fast ? 2 : 1;

    public BossPhase? Phase =>
        Kind.Phases.Count == 0 ? null : Kind.Phases.LastOrDefault(phase => HealthPercent <= phase.FromHealthPercent) ?? Kind.Phases[0];

    public int PhaseNumber => Phase is { } phase ? Kind.Phases.ToList().IndexOf(phase) + 1 : 0;

    public Speed Speed => Phase?.Speed ?? Kind.Speed;

    public BossPower Power => Phase?.Power ?? Kind.Power;

    private int HealthPercent => Stats.MaxHealth == 0 ? 0 : (int)Math.Ceiling(Health * 100.0 / Stats.MaxHealth);

    public int BurnTurnsLeft { get; internal set; }

    public int BurnDamage { get; internal set; }

    public int FrozenTurnsLeft { get; internal set; }

    public int FreezeImmunityTurnsLeft { get; internal set; }

    public int MarkedTurns { get; internal set; }

    public int VenomStacks { get; internal set; }

    public int StaggeredTurns { get; internal set; }

    public bool IsBurning => BurnTurnsLeft > 0;

    public bool IsFrozen => FrozenTurnsLeft > 0;

    public int PowerCharge { get; internal set; }

    public int? SummonerId { get; init; }

    public bool IsSummoned => SummonerId is not null;

    internal void ReturnToStart()
    {
        Position = StartPosition;
        IsAwake = false;
        IsResting = false;
        PowerCharge = 0;
        ClearStatuses();
        RestoreFullHealth();
    }

    internal void ClearStatuses()
    {
        BurnTurnsLeft = 0;
        BurnDamage = 0;
        FrozenTurnsLeft = 0;
        FreezeImmunityTurnsLeft = 0;
        MarkedTurns = 0;
        VenomStacks = 0;
        StaggeredTurns = 0;
    }

    private static Stats ScaledStats(EnemyKind kind, Rarity rarity, int level)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return EnemyScaling.Scale(kind.BaseStats, rarity, level, kind.IsBoss);
    }
}