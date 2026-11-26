using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Levels;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Realtime;

public static class LiveArena
{
    public const int HeroId = 1;

    public static LiveFloor Open(GameSession session, IRandom random)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(random);

        var game = session.Game;
        var player = game.Player;
        var floor = new LiveFloor(game.Map, player.Position, random, game.Biome, new VaultLoot(random), game.FloorNumber);

        floor.AddHero(HeroId, player);

        foreach (var enemy in game.Enemies.Where(enemy => !enemy.IsDead && !enemy.Kind.IsStationary))
            floor.AddMonster(enemy);

        return floor;
    }

    public static LiveHero Enter(LiveFloor floor, int heroId, GameSession session)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(session);

        return floor.AddHero(heroId, session.Game.Player);
    }
}
