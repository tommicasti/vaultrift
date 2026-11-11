using Vaultrift.Api.Contracts;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.World;

namespace Vaultrift.Api.Mapping;

public static class GameMapper
{
    public static GameStateResponse ToResponse(Guid id, Game game) =>
        new(
            id,
            ToRows(game.Map),
            new PlayerResponse(
                game.Player.Position.X,
                game.Player.Position.Y,
                game.Player.Facing,
                game.Player.Health,
                game.Player.Stats.MaxHealth),
            game.Enemies
                .Where(enemy => !enemy.IsDead)
                .Select(enemy => new EnemyResponse(
                    enemy.Id,
                    enemy.Position.X,
                    enemy.Position.Y,
                    enemy.Health,
                    enemy.Stats.MaxHealth,
                    enemy.IsAwake))
                .ToList(),
            game.Deaths);

    public static TurnResponse ToResponse(Guid id, Game game, TurnResult result) =>
        new(
            result.TurnPassed,
            result.Events.Select(ToResponse).ToList(),
            ToResponse(id, game));

    private static EventResponse ToResponse(GameEvent gameEvent) => gameEvent switch
    {
        PlayerMoved e => new("PlayerMoved", X: e.To.X, Y: e.To.Y),
        PlayerTurned => new("PlayerTurned"),
        PlayerAttackedEnemy e => new("PlayerAttackedEnemy", EnemyId: e.EnemyId, Damage: e.Result.Damage, IsCritical: e.Result.IsCritical),
        PlayerAttackedNothing => new("PlayerAttackedNothing"),
        EnemyDefeated e => new("EnemyDefeated", EnemyId: e.EnemyId),
        EnemyAttackedPlayer e => new("EnemyAttackedPlayer", EnemyId: e.EnemyId, Damage: e.Result.Damage, IsCritical: e.Result.IsCritical),
        EnemyWokeUp e => new("EnemyWokeUp", EnemyId: e.EnemyId),
        EnemyMoved e => new("EnemyMoved", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y),
        PlayerDefeated => new("PlayerDefeated"),
        PlayerRespawned e => new("PlayerRespawned", X: e.At.X, Y: e.At.Y),
        _ => throw new ArgumentOutOfRangeException(nameof(gameEvent), gameEvent.GetType().Name, "Unknown game event.")
    };

    private static IReadOnlyList<string> ToRows(DungeonMap map)
    {
        var rows = new List<string>(map.Height);

        for (var y = 0; y < map.Height; y++)
        {
            var row = new char[map.Width];

            for (var x = 0; x < map.Width; x++)
                row[x] = map.GetTile(new Position(x, y)) == TileType.Wall ? '#' : '.';

            rows.Add(new string(row));
        }

        return rows;
    }
}