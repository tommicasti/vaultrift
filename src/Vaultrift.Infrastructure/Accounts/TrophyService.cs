using System.Collections.Concurrent;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Accounts;

public sealed record TrophyStatus(TrophyDefinition Trophy, DateTimeOffset? UnlockedAt, long Progress);

public sealed class TrophyService
{
    private readonly IAccountRepository _accounts;
    private readonly AccountService _service;
    private readonly Func<DateTimeOffset> _clock;
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _unlocked = new();
    private readonly ConcurrentDictionary<Guid, FloorRun> _floors = new();

    public TrophyService(IAccountRepository accounts, AccountService service, Func<DateTimeOffset>? clock = null)
    {
        _accounts = accounts;
        _service = service;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public void StartFloor(Guid gameId, int floor) => _floors[gameId] = new FloorRun(floor, Hurt: false);

    public IReadOnlyList<TrophyDefinition> Record(GameSession session, TurnResult result)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(result);

        var game = session.Game;
        var earned = new List<string>();
        var cleanFloor = TrackFloors(session.Id, game, result.Events);

        if (_service.OwnerOf(session.Id) is not { } accountId)
            return [];

        var unlocked = Unlocked(accountId);
        bool Wants(string id) => !unlocked.ContainsKey(id);

        if (cleanFloor && Wants(TrophyCatalog.Untouchable))
            earned.Add(TrophyCatalog.Untouchable);

        if (Wants(TrophyCatalog.Unbroken) && game.FloorNumber >= TrophyCatalog.UnbrokenFloor && game.Deaths == 0 && !game.IsLost)
            earned.Add(TrophyCatalog.Unbroken);

        long coins = 0;

        foreach (var gameEvent in result.Events)
        {
            switch (gameEvent)
            {
                case EnemyDefeated defeated:
                    if (Wants(TrophyCatalog.FirstBlood))
                        earned.Add(TrophyCatalog.FirstBlood);
                    if (Wants(TrophyCatalog.KingSlayer) && game.Enemies.FirstOrDefault(enemy => enemy.Id == defeated.EnemyId)?.Kind.IsBoss == true)
                        earned.Add(TrophyCatalog.KingSlayer);
                    break;
                case ItemPickedUp { Item.Rarity: Rarity.Exotic }:
                case ItemBought { Item.Rarity: Rarity.Exotic }:
                    if (Wants(TrophyCatalog.Collector))
                        earned.Add(TrophyCatalog.Collector);
                    break;
                case CoinsGained gained when gained.Amount > 0:
                    coins += gained.Amount;
                    break;
                case VaultConquered:
                    earned.AddRange(Conquered(accountId, game).Where(Wants));
                    break;
            }
        }

        if (coins > 0 && Wants(TrophyCatalog.Rich) && _accounts.AddToCounter(accountId, TrophyCatalog.CoinsCounter, coins) >= TrophyCatalog.RichGoal)
            earned.Add(TrophyCatalog.Rich);

        return Grant(accountId, unlocked, earned);
    }

    public IReadOnlyList<TrophyDefinition> RecordLive(Guid accountId, LiveFeats feats)
    {
        ArgumentNullException.ThrowIfNull(feats);

        var unlocked = Unlocked(accountId);
        bool Wants(string id) => !unlocked.ContainsKey(id);
        var earned = new List<string>();

        if (feats.Slew && Wants(TrophyCatalog.FirstBlood))
            earned.Add(TrophyCatalog.FirstBlood);

        if (feats.SlewBoss && Wants(TrophyCatalog.KingSlayer))
            earned.Add(TrophyCatalog.KingSlayer);

        if (feats.FoundExotic && Wants(TrophyCatalog.Collector))
            earned.Add(TrophyCatalog.Collector);

        if (feats.CleanFloor && Wants(TrophyCatalog.Untouchable))
            earned.Add(TrophyCatalog.Untouchable);

        if (feats.FloorReached >= TrophyCatalog.UnbrokenFloor && feats.Falls == 0 && Wants(TrophyCatalog.Unbroken))
            earned.Add(TrophyCatalog.Unbroken);

        if (feats.Coins > 0 && Wants(TrophyCatalog.Rich) && _accounts.AddToCounter(accountId, TrophyCatalog.CoinsCounter, feats.Coins) >= TrophyCatalog.RichGoal)
            earned.Add(TrophyCatalog.Rich);

        if (feats.Conquered is { } difficulty)
        {
            earned.AddRange(CustodeTrophies(difficulty).Where(Wants));
            _accounts.AddToCounter(accountId, TrophyCatalog.ConqueredPrefix + feats.ClassId, 1);

            var counters = _accounts.CountersOf(accountId);
            if (Wants(TrophyCatalog.WeaponMaster) && ClassCatalog.All.All(definition => counters.GetValueOrDefault(TrophyCatalog.ConqueredPrefix + definition.Class.Id) > 0))
                earned.Add(TrophyCatalog.WeaponMaster);
        }

        return earned.Count == 0 ? [] : Grant(accountId, unlocked, earned);
    }

    private List<TrophyDefinition> Grant(Guid accountId, ConcurrentDictionary<string, byte> unlocked, IEnumerable<string> earned)
    {
        var now = _clock();
        var fresh = new List<TrophyDefinition>();

        foreach (var id in earned.Distinct())
        {
            if (!unlocked.TryAdd(id, 0))
                continue;

            try
            {
                if (_accounts.Unlock(accountId, id, now))
                    fresh.Add(TrophyCatalog.Find(id));
            }
            catch
            {
                unlocked.TryRemove(id, out _);
                throw;
            }
        }

        return fresh;
    }

    public int DragonShardTier(Guid accountId)
    {
        var unlocked = Unlocked(accountId);
        return unlocked.ContainsKey(TrophyCatalog.CustodeHardcore) ? 3
            : unlocked.ContainsKey(TrophyCatalog.CustodeHard) ? 2
            : unlocked.ContainsKey(TrophyCatalog.CustodeNormal) ? 1
            : 0;
    }

    public IReadOnlyList<TrophyDefinition> CatchUp(Guid accountId, IEnumerable<HeroSummary> heroes)
    {
        var unlocked = Unlocked(accountId);
        var earned = new List<string>();
        var counted = false;

        foreach (var hero in heroes.Where(hero => hero.IsConquered))
        {
            var trophies = CustodeTrophies(hero.Difficulty).ToList();
            if (trophies.All(unlocked.ContainsKey))
                continue;

            _accounts.AddToCounter(accountId, TrophyCatalog.ConqueredPrefix + hero.ClassId, 1);
            earned.AddRange(trophies);
            counted = true;
        }

        if (counted && ClassCatalog.All.All(definition => _accounts.CountersOf(accountId).GetValueOrDefault(TrophyCatalog.ConqueredPrefix + definition.Class.Id) > 0))
            earned.Add(TrophyCatalog.WeaponMaster);

        return Grant(accountId, unlocked, earned);
    }

    private static IEnumerable<string> CustodeTrophies(Difficulty difficulty)
    {
        yield return TrophyCatalog.KingSlayer;
        yield return TrophyCatalog.CustodeNormal;

        if (difficulty is Difficulty.Hard or Difficulty.Hardcore)
            yield return TrophyCatalog.CustodeHard;

        if (difficulty == Difficulty.Hardcore)
            yield return TrophyCatalog.CustodeHardcore;
    }

    public IReadOnlyList<TrophyStatus> StatusOf(Guid accountId)
    {
        var trophies = _accounts.TrophiesOf(accountId);
        var counters = _accounts.CountersOf(accountId);

        return TrophyCatalog.All
            .Select(trophy => new TrophyStatus(
                trophy,
                trophies.TryGetValue(trophy.Id, out var at) ? at : null,
                trophy.Counter is null ? 0 : Math.Min(trophy.Goal, counters.GetValueOrDefault(trophy.Counter))))
            .ToList();
    }

    private IEnumerable<string> Conquered(Guid accountId, Game game)
    {
        foreach (var trophy in CustodeTrophies(game.Difficulty))
            yield return trophy;

        if (game.Player.Class?.Id is { } classId)
            _accounts.AddToCounter(accountId, TrophyCatalog.ConqueredPrefix + classId, 1);

        var counters = _accounts.CountersOf(accountId);
        if (ClassCatalog.All.All(definition => counters.GetValueOrDefault(TrophyCatalog.ConqueredPrefix + definition.Class.Id) > 0))
            yield return TrophyCatalog.WeaponMaster;
    }

    private bool TrackFloors(Guid gameId, Game game, IReadOnlyList<GameEvent> events)
    {
        var run = _floors.GetOrAdd(gameId, _ => new FloorRun(game.FloorNumber, Hurt: true));
        var clean = false;

        foreach (var gameEvent in events)
        {
            switch (gameEvent)
            {
                case FloorDescended descended:
                    clean |= !run.Hurt && descended.Floor > run.Floor;
                    run = new FloorRun(descended.Floor, Hurt: false);
                    break;
                case Traveled traveled:
                    run = new FloorRun(traveled.Floor, Hurt: true);
                    break;
                case PlayerDefeated or PlayerRespawned:
                    run = run with { Hurt = true };
                    break;
                default:
                    if (HurtsThePlayer(gameEvent))
                        run = run with { Hurt = true };
                    break;
            }
        }

        _floors[gameId] = run;
        return clean;
    }

    private static bool HurtsThePlayer(GameEvent gameEvent) =>
        gameEvent switch
        {
            EnemyAttackedPlayer attack => attack.Result.Damage > 0 && !attack.Result.IsDodged,
            PlayerScorched scorched => scorched.Damage > 0,
            PlayerShardHit shard => shard.Damage > 0,
            PlayerPoisonDamaged poison => poison.Damage > 0,
            PlayerBlownAway blown => blown.Damage > 0,
            _ => false
        };

    private ConcurrentDictionary<string, byte> Unlocked(Guid accountId) =>
        _unlocked.GetOrAdd(accountId, id => new ConcurrentDictionary<string, byte>(_accounts.TrophiesOf(id).Keys.Select(key => new KeyValuePair<string, byte>(key, 0))));

    private sealed record FloorRun(int Floor, bool Hurt);
}
