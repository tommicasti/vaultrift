using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const double MonsterRespawnChance = 0.05;
    public const int MonsterRespawnDistance = 9;

    private void RespawnMonster(List<GameEvent> events)
    {
        var candidates = _enemies.Where(CanRespawn).ToList();

        if (candidates.Count == 0 || _random.NextDouble() >= MonsterRespawnChance)
            return;

        var monster = _random.Pick(candidates);
        monster.ReturnToStart();
        events.Add(new EnemyRespawned(monster.Id));
    }

    private bool CanRespawn(Enemy enemy) =>
        enemy.IsDead &&
        !enemy.Kind.IsBoss &&
        !enemy.Kind.IsStationary &&
        !enemy.IsSummoned &&
        Distance(enemy.StartPosition, Player.Position) >= MonsterRespawnDistance &&
        IsFree(enemy.StartPosition);
}
