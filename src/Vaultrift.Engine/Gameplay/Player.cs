using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Player : Creature
{
    public Player(Position position, Stats stats)
        : base(position, stats)
    {
        Facing = Direction.Down;
    }

    public Direction Facing { get; internal set; }

    public Inventory Inventory { get; } = new();
}