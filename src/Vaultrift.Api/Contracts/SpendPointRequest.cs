using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Api.Contracts;

public sealed class SpendPointRequest
{
    public required StatPoint Stat { get; init; }
}