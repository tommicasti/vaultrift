using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public sealed class LiveMonster
{
    internal LiveMonster(Enemy enemy)
    {
        Source = enemy;
        Id = enemy.Id;
        Kind = enemy.Kind;
        Rarity = enemy.Rarity;
        Level = enemy.Level;
        Stats = enemy.Stats;
        Home = new Vector(enemy.Position.X, enemy.Position.Y);
        Position = Home;
        MaxHealth = Stats.MaxHealth;
        Health = MaxHealth;
    }

    public int Id { get; }

    public Enemy Source { get; }

    public EnemyKind Kind { get; }

    public Rarity Rarity { get; }

    public int Level { get; }

    public Stats Stats { get; }

    public Vector Home { get; }

    public Vector Position { get; internal set; }

    public Vector Facing { get; internal set; } = new(0, 1);

    public int Health { get; internal set; }

    public int MaxHealth { get; private set; }

    public bool IsDead => Health == 0;

    public bool IsAwake { get; internal set; }

    public bool IsMoving { get; internal set; }

    public double Radius => Kind.IsBoss ? LiveRules.BossRadius : LiveRules.MonsterRadius;

    public bool IsBurning => BurnSecondsLeft > 0;

    public BossPhase? Phase =>
        Kind.Phases.Count == 0 ? null : Kind.Phases.LastOrDefault(phase => HealthPercent <= phase.FromHealthPercent) ?? Kind.Phases[0];

    public int PhaseNumber => Phase is { } phase ? Kind.Phases.ToList().IndexOf(phase) + 1 : 0;

    public Speed Speed => Phase?.Speed ?? Kind.Speed;

    public BossPower Power => Phase?.Power ?? Kind.Power;

    public int? SummonerId { get; internal set; }

    internal int PowerCharge { get; set; }

    internal int AnnouncedPhase { get; set; } = 1;

    private int HealthPercent => MaxHealth == 0 ? 0 : (int)Math.Ceiling(Health * 100.0 / MaxHealth);

    internal double FrozenUntil { get; set; } = double.NegativeInfinity;

    internal double StaggeredUntil { get; set; } = double.NegativeInfinity;

    public bool IsMarked(double time) => time < MarkedUntil;

    internal double MarkedUntil { get; set; } = double.NegativeInfinity;

    public int VenomStacks { get; internal set; }

    internal int VenomOwnerId { get; set; }

    internal double LastWallStruckAt { get; set; } = double.NegativeInfinity;

    internal double FreezeImmuneUntil { get; set; } = double.NegativeInfinity;

    internal int BurnSecondsLeft { get; set; }

    internal int BurnDamage { get; set; }

    internal int BurnOwnerId { get; set; }

    internal double AttackReadyAt { get; set; }

    internal double? WindupEndsAt { get; set; }

    internal int? TargetId { get; set; }

    internal Vector Heading { get; set; } = Vector.Zero;

    internal void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);

    internal void Reset()
    {
        Position = Home;
        IsAwake = false;
        IsMoving = false;
        WindupEndsAt = null;
        TargetId = null;
        Health = MaxHealth;
        BurnSecondsLeft = 0;
        FrozenUntil = double.NegativeInfinity;
        StaggeredUntil = double.NegativeInfinity;
        MarkedUntil = double.NegativeInfinity;
        VenomStacks = 0;
    }

    internal void Rescale(double scale)
    {
        if (IsDead)
            return;

        var scaled = Math.Max(1, (int)Math.Round(Stats.MaxHealth * scale));
        Health = Math.Max(1, (int)Math.Round((double)Health * scaled / MaxHealth));
        MaxHealth = scaled;
    }
}
