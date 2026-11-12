using Vaultrift.Engine.World;

namespace Vaultrift.Api.Contracts;

public sealed record GameStateResponse(
    Guid Id,
    IReadOnlyList<string> Map,
    PlayerResponse Player,
    IReadOnlyList<EnemyResponse> Enemies,
    int Deaths);

public sealed record PlayerResponse(int X, int Y, Direction Facing, int Health, int MaxHealth);

public sealed record EnemyResponse(int Id, int X, int Y, int Health, int MaxHealth, bool IsAwake);

public sealed record TurnResponse(bool TurnPassed, IReadOnlyList<EventResponse> Events, GameStateResponse State);

public sealed record EventResponse(
    string Type,
    int? EnemyId = null,
    int? Damage = null,
    bool? IsCritical = null,
    bool? IsDodged = null,
    int? X = null,
    int? Y = null);