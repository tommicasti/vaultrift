namespace Vaultrift.Engine.Gameplay;

public sealed record TurnResult(bool TurnPassed, IReadOnlyList<GameEvent> Events);