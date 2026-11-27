using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public sealed class LiveHero
{
    internal LiveHero(int id, Player player, string classId, Speed speed, Vector position, Guid owner)
    {
        Id = id;
        Owner = owner;
        Player = player;
        ClassId = classId;
        Speed = speed;
        Position = position;
    }

    public int Id { get; }

    public Guid Owner { get; }

    public Player Player { get; }

    public string Name => Player.Name;

    public string ClassId { get; }

    public Stats Stats => Player.CombatStats;

    public Speed Speed { get; }

    public Vector Position { get; internal set; }

    public Vector Facing { get; internal set; } = new(0, 1);

    public int Health => Player.Health;

    public int MaxHealth => Player.Stats.MaxHealth;

    public bool IsDead => Player.Health == 0;

    public bool IsMoving { get; internal set; }

    public bool IsAway { get; internal set; }

    public bool IsSprinting { get; internal set; }

    internal bool IsTargetable => !IsDead && !IsAway && !Player.IsInvisible;

    public int Version { get; private set; }

    public int Deaths { get; internal set; }

    internal HeroInput Input { get; set; } = HeroInput.Idle;

    internal double AttackReadyAt { get; set; }

    internal double RespawnAt { get; set; }

    internal double FullNoticeAt { get; set; }

    internal void TakeDamage(int amount)
    {
        if (!IsAway)
            Player.TakeDamage(amount);
    }

    public void Touch() => Version++;

    internal int ComboTargetId { get; set; } = -1;

    internal int ComboCount { get; set; }

    internal int Riposte { get; set; }

    internal double ShadowstepReadyAt { get; set; }

    public ExoticPower? Exotic => (Player.Weapon?.Definition as WeaponDefinition)?.Exotic;

    public bool Wields(ExoticPower power) => Exotic == power;
}
