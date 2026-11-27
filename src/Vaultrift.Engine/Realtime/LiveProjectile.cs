using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public enum ProjectileKind
{
    Arrow,
    Spell
}

public sealed class LiveProjectile
{
    internal LiveProjectile(int id, int ownerId, ProjectileKind kind, Vector origin, Vector direction, double speed, double range)
    {
        Id = id;
        OwnerId = ownerId;
        Kind = kind;
        Origin = origin;
        Position = origin;
        Direction = direction;
        Speed = speed;
        RangeLeft = range;
    }

    public int Id { get; }

    public int OwnerId { get; }

    public ProjectileKind Kind { get; }

    public Vector Origin { get; }

    public Vector Position { get; internal set; }

    public Vector Direction { get; }

    public double Speed { get; }

    public Element? Element => Spell?.Element ?? Weapon?.Element;

    internal double RangeLeft { get; set; }

    internal ItemInstance? Weapon { get; init; }

    internal SpellDefinition? Spell { get; init; }

    internal Rarity Rarity { get; init; } = Rarity.Common;

    internal int Size { get; init; }

    internal bool Pierces { get; init; }

    internal int Multiplier { get; init; } = 1;

    internal int PierceCount { get; set; }

    internal HashSet<int> Struck { get; } = [];

    public int PowerPercent { get; init; } = 100;

    public int PiercePercent { get; init; } = LiveRules.PiercePercent;
}
