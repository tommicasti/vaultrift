using System.ComponentModel.DataAnnotations;
using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Api.Contracts;

public sealed class CreateGameRequest
{
    public required string ClassId { get; init; }

    [StringLength(Player.MaxNameLength)]
    public string? Name { get; init; }

    public Difficulty Difficulty { get; init; } = Difficulty.Normal;
}
