using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public sealed record EnemyKind
{
    public EnemyKind(
        string id,
        string name,
        Stats baseStats,
        Speed speed = Speed.Normal,
        bool passesThroughWalls = false,
        Element? weakness = null,
        Element? resistance = null,
        Element? attackElement = null,
        bool isBoss = false,
        bool isStationary = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(baseStats);

        if (weakness is not null && weakness == resistance)
            throw new ArgumentException("A kind cannot be weak and resistant to the same element.", nameof(resistance));

        Id = id;
        Name = name;
        BaseStats = baseStats;
        Speed = speed;
        PassesThroughWalls = passesThroughWalls;
        Weakness = weakness;
        Resistance = resistance;
        AttackElement = attackElement;
        IsBoss = isBoss;
        IsStationary = isStationary;
    }

    public string Id { get; }

    public string Name { get; }

    public Stats BaseStats { get; }

    public Speed Speed { get; }

    public bool PassesThroughWalls { get; }

    public Element? Weakness { get; }

    public Element? Resistance { get; }

    public Element? AttackElement { get; }

    public bool IsBoss { get; }

    public bool IsStationary { get; }
}
