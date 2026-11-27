using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Accounts;

public class TrophyServiceTests
{
    private readonly MemoryAccountRepository _accounts = new();
    private readonly AccountService _service;
    private readonly TrophyService _trophies;
    private readonly Account _account;

    public TrophyServiceTests()
    {
        _service = new AccountService(_accounts, new MemorySaveRepository(), iterations: 1000);
        _trophies = new TrophyService(_accounts, _service);
        _account = _service.Register("Tommaso", "password123", null).Account!;
    }

    private GameSession OwnedGame(Difficulty difficulty = Difficulty.Normal, string classId = "knight")
    {
        var session = GameSession.Start(Guid.NewGuid(), SaveCode.Generate(), ClassCatalog.Find(classId)!, "Hero", 3, difficulty);
        _service.Link(_account.Id, session.Id);
        _trophies.StartFloor(session.Id, session.Game.FloorNumber);
        return session;
    }

    private IReadOnlyList<string> Play(GameSession session, params GameEvent[] events) =>
        _trophies.Record(session, new TurnResult(true, events)).Select(trophy => trophy.Id).ToList();

    [Fact]
    public void TheFirstKill_UnlocksFirstBloodOnlyOnce()
    {
        var game = OwnedGame();

        Assert.Equal([TrophyCatalog.FirstBlood], Play(game, new EnemyDefeated(999)));
        Assert.Empty(Play(game, new EnemyDefeated(998)));
        Assert.NotNull(_trophies.StatusOf(_account.Id).Single(status => status.Trophy.Id == TrophyCatalog.FirstBlood).UnlockedAt);
    }

    [Fact]
    public void GamesWithoutAnAccount_EarnNothing()
    {
        var session = GameSession.Start(Guid.NewGuid(), SaveCode.Generate(), ClassCatalog.Find("knight")!, "Hero", 3);

        Assert.Empty(_trophies.Record(session, new TurnResult(true, [new EnemyDefeated(1)])));
    }

    [Fact]
    public void AFloorClearedWithoutDamage_IsUntouchable()
    {
        var game = OwnedGame();

        Assert.Contains(TrophyCatalog.Untouchable, Play(game, new FloorDescended(2, new Position(1, 1))));
    }

    [Fact]
    public void TakingAHit_SpoilsTheFloor()
    {
        var game = OwnedGame();

        Play(game, new EnemyAttackedPlayer(5, new AttackResult(3, false)));

        Assert.DoesNotContain(TrophyCatalog.Untouchable, Play(game, new FloorDescended(2, new Position(1, 1))));
        Assert.Contains(TrophyCatalog.Untouchable, Play(game, new EnemyAttackedPlayer(5, AttackResult.Dodged), new FloorDescended(3, new Position(1, 1))));
    }

    [Fact]
    public void CoinsAddUpAcrossGames_ToFilthyRich()
    {
        Assert.Empty(Play(OwnedGame(), new CoinsGained(60_000)));
        Assert.Equal([TrophyCatalog.Rich], Play(OwnedGame(), new CoinsGained(40_000)));
        Assert.Equal(TrophyCatalog.RichGoal, _trophies.StatusOf(_account.Id).Single(status => status.Trophy.Id == TrophyCatalog.Rich).Progress);
    }

    [Fact]
    public void BeatingTheCustodeOnHardcore_UnlocksEveryCustodeTrophy()
    {
        var unlocked = Play(OwnedGame(Difficulty.Hardcore), new VaultConquered());

        Assert.Contains(TrophyCatalog.CustodeNormal, unlocked);
        Assert.Contains(TrophyCatalog.CustodeHard, unlocked);
        Assert.Contains(TrophyCatalog.CustodeHardcore, unlocked);
        Assert.Contains(TrophyCatalog.KingSlayer, unlocked);
        Assert.DoesNotContain(TrophyCatalog.WeaponMaster, unlocked);
    }

    [Fact]
    public void BeatingTheCustodeWithEveryClass_MakesAWeaponMaster()
    {
        var classes = ClassCatalog.All.Select(definition => definition.Class.Id).ToList();
        var last = new List<string>();

        foreach (var classId in classes)
            last = [.. Play(OwnedGame(classId: classId), new VaultConquered())];

        Assert.Contains(TrophyCatalog.WeaponMaster, last);
    }

    [Fact]
    public void TheDragonShard_GrowsWithTheHardestCustodeBeaten()
    {
        Assert.Equal(0, _trophies.DragonShardTier(_account.Id));

        Play(OwnedGame(Difficulty.Normal), new VaultConquered());
        Assert.Equal(1, _trophies.DragonShardTier(_account.Id));

        Play(OwnedGame(Difficulty.Hard), new VaultConquered());
        Assert.Equal(2, _trophies.DragonShardTier(_account.Id));
    }

    [Fact]
    public void HeroesThatBeatTheCustodeBeforeTrophiesExisted_CatchUp()
    {
        var hero = new HeroSummary(Guid.NewGuid(), "ABCD-EFGH", "Tommy", "rogue", 21, 23, Difficulty.Hard, false, true, 0, DateTimeOffset.UtcNow);

        var granted = _trophies.CatchUp(_account.Id, [hero]).Select(trophy => trophy.Id).ToList();

        Assert.Contains(TrophyCatalog.CustodeHard, granted);
        Assert.Equal(2, _trophies.DragonShardTier(_account.Id));
        Assert.Empty(_trophies.CatchUp(_account.Id, [hero]));
    }

    private IReadOnlyList<string> Live(LiveFeats feats) =>
        _trophies.RecordLive(_account.Id, feats).Select(trophy => trophy.Id).ToList();

    [Fact]
    public void WorldsAwardTheSameTrophiesAsTheTurnBasedGame()
    {
        Assert.Equal([TrophyCatalog.FirstBlood, TrophyCatalog.KingSlayer], Live(new LiveFeats("knight") { Slew = true, SlewBoss = true }));
        Assert.Empty(Live(new LiveFeats("knight") { Slew = true, SlewBoss = true }));
        Assert.Equal([TrophyCatalog.Collector], Live(new LiveFeats("knight") { FoundExotic = true }));
        Assert.Equal([TrophyCatalog.Untouchable], Live(new LiveFeats("knight") { CleanFloor = true, FloorReached = 2 }));
    }

    [Fact]
    public void ReachingTheTenthFloorWithoutFallingIsUnbroken()
    {
        Assert.Empty(Live(new LiveFeats("knight") { FloorReached = TrophyCatalog.UnbrokenFloor, Falls = 1 }));
        Assert.Equal([TrophyCatalog.Unbroken], Live(new LiveFeats("knight") { FloorReached = TrophyCatalog.UnbrokenFloor }));
    }

    [Fact]
    public void CoinsFromWorldsCountTowardsRich()
    {
        Assert.Empty(Live(new LiveFeats("knight") { Coins = TrophyCatalog.RichGoal - 1 }));
        Assert.Equal([TrophyCatalog.Rich], Live(new LiveFeats("knight") { Coins = 1 }));
    }

    [Fact]
    public void ConqueringAHardWorldGrantsTheCustodeTrophiesUpToHard()
    {
        var earned = Live(new LiveFeats("ranger") { Conquered = Difficulty.Hard });

        Assert.Contains(TrophyCatalog.CustodeNormal, earned);
        Assert.Contains(TrophyCatalog.CustodeHard, earned);
        Assert.DoesNotContain(TrophyCatalog.CustodeHardcore, earned);
        Assert.Equal(2, _trophies.DragonShardTier(_account.Id));
    }
}
