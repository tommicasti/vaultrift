using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Engine.Realtime;

public sealed class LiveHero
{
    internal LiveHero(int id, Player player, string classId, Speed speed, Vector position)
    {
        Id = id;
        Player = player;
        ClassId = classId;
        Speed = speed;
        Position = position;
    }

    public int Id { get; }

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

    public int Version { get; private set; }

    public int Deaths { get; internal set; }

    internal HeroInput Input { get; set; } = HeroInput.Idle;

    internal double AttackReadyAt { get; set; }

    internal double RespawnAt { get; set; }

    internal double FullNoticeAt { get; set; }

    internal void TakeDamage(int amount) => Player.TakeDamage(amount);

    internal void Touch() => Version++;
}
