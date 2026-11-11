using Vaultrift.Engine.Combat;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Enemy : Creature
{
    public Enemy(int id, Position position, Stats stats)
        : base(position, stats)
    {
        Id = id;
    }

    public int Id { get; }

    public bool IsAwake { get; internal set; }
}