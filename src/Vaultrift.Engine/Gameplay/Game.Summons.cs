using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int MinionsPerSummon = 2;
    public const int MaxMinions = 4;

    private void SummonMinions(Enemy summoner, List<GameEvent> events)
    {
        if (summoner.Kind.Minion is not { } minion)
            return;

        var alive = _enemies.Count(enemy => !enemy.IsDead && enemy.SummonerId == summoner.Id);
        var count = Math.Min(MinionsPerSummon, MaxMinions - alive);
        var spots = Around(summoner.Position)
            .Where(spot => IsFree(spot) && !Map.IsLava(spot))
            .Take(count)
            .ToList();

        foreach (var spot in spots)
        {
            var summoned = new Enemy(NextEnemyId(), spot, minion, Rarity.Common, summoner.Level)
            {
                SummonerId = summoner.Id,
                IsAwake = true
            };

            _enemies.Add(summoned);
            events.Add(new EnemySummoned(summoner.Id, summoned.Id, spot));
        }
    }

    private void WitherMinions(Enemy summoner, List<GameEvent> events)
    {
        foreach (var minion in _enemies.Where(enemy => !enemy.IsDead && enemy.SummonerId == summoner.Id).ToList())
        {
            minion.TakeDamage(minion.Health);
            events.Add(new EnemyWithered(minion.Id));
        }
    }

    private void DismissMinions() => _enemies.RemoveAll(enemy => enemy.IsSummoned);

    private int NextEnemyId() =>
        _enemies.Select(enemy => enemy.Id)
            .Concat(_chests.Values.Select(chest => chest.Mimic?.Id).OfType<int>())
            .DefaultIfEmpty(0)
            .Max() + 1;
}
