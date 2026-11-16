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

    public int StepsPerTurn => Kind.Speed == Speed.Fast ? 2 : 1;

    private static Stats ScaledStats(EnemyKind kind, Rarity rarity, int level)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return EnemyScaling.Scale(kind.BaseStats, rarity, level);
    }
}