using Vaultrift.Engine.World;

namespace Vaultrift.Api.Contracts;

public sealed class MoveRequest
{
    public required Direction Direction { get; init; }
}