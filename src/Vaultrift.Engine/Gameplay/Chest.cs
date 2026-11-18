using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed record Chest(Position Position, Enemy? Mimic = null)
{
    public bool IsMimic => Mimic is not null;
}
