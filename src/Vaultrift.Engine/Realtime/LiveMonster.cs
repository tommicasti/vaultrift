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

    internal double FrozenUntil { get; set; } = double.NegativeInfinity;

    internal double FreezeImmuneUntil { get; set; } = double.NegativeInfinity;

    internal int BurnSecondsLeft { get; set; }

    internal int BurnDamage { get; set; }

    internal int BurnOwnerId { get; set; }

    internal double AttackReadyAt { get; set; }

    internal double? WindupEndsAt { get; set; }

    internal int? TargetId { get; set; }

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
