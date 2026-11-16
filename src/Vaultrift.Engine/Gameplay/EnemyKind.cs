using Vaultrift.Engine.Combat;

namespace Vaultrift.Engine.Gameplay;

public sealed record EnemyKind
{
    public EnemyKind(string id, string name, Stats baseStats, Speed speed = Speed.Normal, bool passesThroughWalls = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(baseStats);

        Id = id;
        Name = name;
        BaseStats = baseStats;
        Speed = speed;
        PassesThroughWalls = passesThroughWalls;
    }

    public string Id { get; }

    public string Name { get; }

    public Stats BaseStats { get; }

    public Speed Speed { get; }

    public bool PassesThroughWalls { get; }
}