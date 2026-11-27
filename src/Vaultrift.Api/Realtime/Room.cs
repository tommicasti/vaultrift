using System.Security.Cryptography;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Mapping;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Realtime;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Realtime;

public sealed class Room
{
    public const double HeroStateSeconds = 0.2;
    public const double KeyframeSeconds = 5;
    public const double SaveSeconds = 15;
    public const double WorldSaveSeconds = 60;
    public static readonly TimeSpan ResumeGrace = TimeSpan.FromSeconds(30);
    public const string HardcoreFallMessage = "Your hero fell: this Hardcore world is gone. The hero keeps their level and starts over.";

    private readonly Lock _gate = new();
    private readonly WorldSession _world;
    private readonly IRandom _random;
    private LiveFloor _floor;
    private readonly IVaultSaver _store;
    private double _worldSavedAt;
    private bool _lost;
    private int? _travelTo;
    private readonly Func<Guid, LiveFeats, IReadOnlyList<TrophyDefinition>>? _trophies;
    private readonly Dictionary<string, Member> _members = [];
    private int _nextHeroId = LiveArena.FirstHeroId;
    private int _itemsVersion;
    private int _partyVersion;

    private sealed class Member(string connectionId, HeroSession session, LiveHero hero)
    {
        public string ConnectionId { get; set; } = connectionId;

        public string ResumeToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        public DateTimeOffset? AwayUntil { get; set; }

        public HeroSession Session { get; } = session;

        public LiveHero Hero { get; set; } = hero;

        public int SentHeroVersion { get; set; } = -1;

        public double HeroSentAt { get; set; } = double.NegativeInfinity;

        public int SentPartyVersion { get; set; } = -1;

        public Dictionary<int, LiveMonsterView>? SentMonsters { get; set; }

        public double MonstersSentAt { get; set; }

        public int SavedHeroVersion { get; set; }

        public double SavedAt { get; set; }

        public bool HurtOnFloor { get; set; }

        public long PendingCoins { get; set; }

        public List<TrophyResponse> Trophies { get; } = [];
    }

    public Room(
        string code,
        string hostConnectionId,
        WorldSession world,
        bool isShared,
        IVaultSaver store,
        DateTimeOffset now,
        Func<Guid, LiveFeats, IReadOnlyList<TrophyDefinition>>? trophies = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostConnectionId);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(store);

        Code = code;
        HostConnectionId = hostConnectionId;
        IsShared = isShared;
        _world = world;
        _random = world.Random;
        _store = store;
        _trophies = trophies;
        _floor = LiveArena.Open(world);
        LastInputAt = now;
    }

    public bool IsShared { get; }

    public Guid WorldId => _world.Id;

    public string? EndedMessage { get; private set; }

    public string Code { get; }

    public string HostConnectionId { get; private set; }

    public DateTimeOffset LastInputAt { get; private set; }

    public IReadOnlyList<string> Connections
    {
        get
        {
            lock (_gate)
                return _members.Keys.ToList();
        }
    }

    public bool IsFull
    {
        get
        {
            lock (_gate)
                return _members.Count >= LiveRules.MaxHeroes;
        }
    }

    public LiveWelcome? Enter(string connectionId, HeroSession session, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(session);

        lock (_gate)
        {
            if (!_members.ContainsKey(connectionId))
            {
                if (_members.Count >= LiveRules.MaxHeroes)
                    return null;

                session.MarkPlayed(_world.Difficulty);
                var hero = LiveArena.Enter(_floor, _nextHeroId++, session);
                _members[connectionId] = new Member(connectionId, session, hero) { SavedHeroVersion = hero.Version, SavedAt = _floor.Time };
                _partyVersion++;
            }

            LastInputAt = now;
            return Welcome(_members[connectionId]);
        }
    }

    public bool Park(string connectionId, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_members.TryGetValue(connectionId, out var member))
                return false;

            member.AwayUntil = now + ResumeGrace;
            _floor.SetAway(member.Hero.Id, true);
            FlushCoins(member);
            _store.Save(member.Session);
            _partyVersion++;
            return true;
        }
    }

    public bool HasToken(string token)
    {
        lock (_gate)
            return _members.Values.Any(member => member.ResumeToken == token);
    }

    public (string Previous, LiveWelcome Welcome)? Resume(string token, Guid accountId, string connectionId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            if (_members.Values.FirstOrDefault(member => member.ResumeToken == token && member.Session.AccountId == accountId) is not { } member)
                return null;

            var previous = member.ConnectionId;
            _members.Remove(previous);
            member.ConnectionId = connectionId;
            _members[connectionId] = member;

            if (HostConnectionId == previous)
                HostConnectionId = connectionId;

            member.AwayUntil = null;
            _floor.SetAway(member.Hero.Id, false);
            LastInputAt = now;
            _partyVersion++;
            return (previous, Welcome(member));
        }
    }

    public IReadOnlyList<string> ExpiredAway(DateTimeOffset now)
    {
        lock (_gate)
            return _members.Values.Where(member => member.AwayUntil <= now).Select(member => member.ConnectionId).ToList();
    }

    public void Leave(string connectionId)
    {
        lock (_gate)
        {
            if (!_members.Remove(connectionId, out var member))
                return;

            _floor.RemoveHero(member.Hero.Id);
            FlushCoins(member);
            _store.Save(member.Session);
            _partyVersion++;
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            foreach (var member in _members.Values)
            {
                _floor.RemoveHero(member.Hero.Id);
                FlushCoins(member);
                _store.Save(member.Session);
            }

            if (!_lost)
            {
                _world.SetGraves(_floor.FloorNumber, LiveArena.GravesOf(_floor));
                _store.Save(_world);
            }

            _members.Clear();
            _partyVersion++;
        }
    }

    public Guid? HeroOf(string connectionId)
    {
        lock (_gate)
            return _members.TryGetValue(connectionId, out var member) ? member.Session.Id : null;
    }

    public void SetInput(string connectionId, HeroInput input, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_members.TryGetValue(connectionId, out var member))
                return;

            _floor.SetInput(member.Hero.Id, input);
            LastInputAt = now;
        }
    }

    public IReadOnlyList<(string ConnectionId, LiveSnapshot Snapshot)> Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ticks, 1);

        lock (_gate)
        {
            var events = new List<LiveEvent>();

            if (_travelTo is { } destination)
            {
                _travelTo = null;
                return MoveTo(destination, viaStairs: false);
            }

            for (var tick = 0; tick < ticks && EndedMessage is null; tick++)
            {
                var step = _floor.Step();
                events.AddRange(step);
                Notice(step);

                if (_world.Difficulty == Difficulty.Hardcore && step.OfType<HeroFell>().Any())
                    LoseWorld();
                else if (HostReachedStairs(step))
                    return MoveTo(_floor.FloorNumber + 1, viaStairs: true);
                else
                    Remember(step, events);
            }

            if (EndedMessage is null)
                SaveChangedHeroes();

            IReadOnlyList<LiveItemView>? items = null;

            if (_floor.ItemsVersion != _itemsVersion)
            {
                _itemsVersion = _floor.ItemsVersion;
                items = LiveMapper.Items(_floor);
            }

            var shared = LiveMapper.Snapshot(_floor, events, items: items);
            IReadOnlyList<LivePartyMember>? party = null;
            var sends = new List<(string, LiveSnapshot)>();

            foreach (var member in _members.Values)
            {
                if (member.AwayUntil is not null)
                    continue;

                LiveHeroState? hero = null;

                if (member.Hero.Version != member.SentHeroVersion && _floor.Time - member.HeroSentAt >= HeroStateSeconds)
                {
                    member.SentHeroVersion = member.Hero.Version;
                    member.HeroSentAt = _floor.Time;
                    hero = LiveMapper.HeroState(_world, member.Session, _floor, member.Hero);
                }

                IReadOnlyList<LivePartyMember>? memberParty = null;

                if (member.SentPartyVersion != _partyVersion)
                {
                    member.SentPartyVersion = _partyVersion;
                    memberParty = party ??= Party();
                }

                sends.Add((member.ConnectionId, MonstersFor(member, shared) with { Hero = hero, Party = memberParty, Trophies = TakeTrophies(member) }));
            }

            return sends;
        }
    }

    public LiveActionResponse? Act(string connectionId, Func<LiveAct, TurnResult> action, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_gate)
        {
            if (!_members.TryGetValue(connectionId, out var member))
                return null;

            LastInputAt = now;
            var result = action(new LiveAct(_floor, member.Hero, member.Session.Game));

            if (result.Events.OfType<ItemBought>().Any(bought => bought.Item.Rarity == Rarity.Exotic))
                Grant(member, new LiveFeats(member.Session.ClassId) { FoundExotic = true });

            var response = GameMapper.ToResponse(member.Session, result);
            member.Hero.Touch();
            member.SentHeroVersion = member.Hero.Version;
            _partyVersion++;
            return new LiveActionResponse(response.Events, LiveMapper.HeroState(_world, member.Session, _floor, member.Hero));
        }
    }

    public StatsPreviewResponse? Preview(string connectionId, int entryId, EquipmentSlot? hand)
    {
        lock (_gate)
        {
            if (!_members.TryGetValue(connectionId, out var member))
                return null;

            return ToResponse(member.Hero.Player.PreviewEquip(entryId, hand, VaultContent.Instance));
        }
    }

    public StatsPreviewResponse? PreviewOffer(string connectionId, int offerId, EquipmentSlot? hand)
    {
        lock (_gate)
        {
            if (!_members.TryGetValue(connectionId, out var member))
                return null;

            if (_floor.Merchant?.Offers.FirstOrDefault(offer => offer.Id == offerId) is not { } found)
                return new StatsPreviewResponse(EquipResult.ItemNotFound, 0, 0, 0, 0, 0, 0, 0, null);

            return ToResponse(member.Hero.Player.PreviewItem(found.Item, hand, VaultContent.Instance));
        }
    }

    private static StatsPreviewResponse ToResponse(StatsPreview preview) =>
        new(
            preview.Result,
            preview.MaxHealth,
            preview.MaxMana,
            preview.Attack,
            preview.Defense,
            preview.Evasion,
            preview.Critical,
            preview.Block,
            preview.Resistance);

    private bool HostReachedStairs(IReadOnlyList<LiveEvent> step) =>
        _members.TryGetValue(HostConnectionId, out var host)
        && step.OfType<HeroAtStairs>().Any(arrival => arrival.HeroId == host.Hero.Id);

    private void LoseWorld()
    {
        _lost = true;
        EndedMessage = HardcoreFallMessage;

        foreach (var member in _members.Values)
        {
            member.Session.LoseEverything();
            _store.Save(member.Session);
        }

        _store.Lose(_world);
    }

    public string? Travel(string connectionId, int destination)
    {
        lock (_gate)
        {
            if (!_members.TryGetValue(connectionId, out var member))
                return "You are not in a world.";

            if (connectionId != HostConnectionId)
                return "Only the host can choose where the party travels.";

            if (!_floor.IsAtWaystone(member.Hero.Id))
                return "Stand next to the waystone to travel.";

            if (destination == _floor.FloorNumber || destination < 1 || destination > Math.Min(_world.DeepestFloor, LiveArena.LastFloor))
                return "The waystone cannot take you there.";

            _travelTo = destination;
            return null;
        }
    }

    private void Remember(IReadOnlyList<LiveEvent> step, List<LiveEvent> events)
    {
        var worldChanged = false;

        foreach (var happened in step)
        {
            switch (happened)
            {
                case FloorCleared cleared:
                    _world.MarkCleared(cleared.Floor);
                    worldChanged = true;
                    break;
                case LiveVaultConquered:
                    if (!_world.IsConquered)
                        GrantDragonWeapons(events);
                    _world.Conquer();
                    worldChanged = true;
                    break;
                case LiveTotemAwakened:
                    _world.UseTotem();
                    worldChanged = true;
                    break;
                case LiveGraveDug or LiveGraveRecovered or LiveGraveLost:
                    _world.SetGraves(_floor.FloorNumber, LiveArena.GravesOf(_floor));
                    worldChanged = true;
                    break;
            }
        }

        if (!worldChanged)
            return;

        foreach (var member in _members.Values)
            member.Hero.Touch();

        _worldSavedAt = _floor.Time;
        _store.Save(_world);
    }

    private void GrantDragonWeapons(List<LiveEvent> events)
    {
        foreach (var member in _members.Values)
        {
            foreach (var weapon in DragonReward.For(member.Session.ClassId, _world.Difficulty, _random))
            {
                var stashed = !member.Hero.Player.Inventory.TryAddBound(weapon);

                if (stashed)
                    _store.Keep(member.Session.AccountId, weapon);

                events.Add(new LiveRewardGranted(member.Hero.Id, weapon.Name, stashed));
            }

            member.Hero.Touch();
            _store.Save(member.Session);
        }
    }

    private void Notice(IReadOnlyList<LiveEvent> step)
    {
        if (step.Count == 0)
            return;

        var feats = new Dictionary<Member, LiveFeats>();
        LiveFeats FeatsOf(Member member) => feats.TryGetValue(member, out var found) ? found : feats[member] = new LiveFeats(member.Session.ClassId);

        foreach (var happened in step)
        {
            switch (happened)
            {
                case MonsterSlain slain when MemberOf(slain.HeroId) is { } slayer:
                    FeatsOf(slayer).Slew = true;
                    if (_floor.Monsters.FirstOrDefault(monster => monster.Id == slain.MonsterId)?.Kind.IsBoss == true)
                        FeatsOf(slayer).SlewBoss = true;
                    break;
                case LootPickedUp { Item.Rarity: Rarity.Exotic } picked when MemberOf(picked.HeroId) is { } finder:
                    FeatsOf(finder).FoundExotic = true;
                    break;
                case HeroRewarded rewarded when MemberOf(rewarded.HeroId) is { } earner:
                    earner.PendingCoins += rewarded.Coins;
                    break;
                case ChestLooted looted when MemberOf(looted.HeroId) is { } looter:
                    looter.PendingCoins += looted.Coins;
                    break;
                case HeroFell fell when MemberOf(fell.HeroId) is { } fallen:
                    fallen.HurtOnFloor = true;
                    fallen.Session.RecordFall();
                    break;
                case HeroHit { Result.Missed: false, Result.Damage: > 0 } hit when MemberOf(hit.HeroId) is { } wounded:
                    wounded.HurtOnFloor = true;
                    break;
                case HeroScorched { Damage: > 0 } scorched when MemberOf(scorched.HeroId) is { } burned:
                    burned.HurtOnFloor = true;
                    break;
                case HeroShardHit { Damage: > 0 } shard when MemberOf(shard.HeroId) is { } cut:
                    cut.HurtOnFloor = true;
                    break;
                case HeroPoisonDamaged { Damage: > 0 } poison when MemberOf(poison.HeroId) is { } sick:
                    sick.HurtOnFloor = true;
                    break;
                case HeroBlownAway { Damage: > 0 } blown when MemberOf(blown.HeroId) is { } thrown:
                    thrown.HurtOnFloor = true;
                    break;
                case LiveVaultConquered:
                    foreach (var member in _members.Values)
                        FeatsOf(member).Conquered = _world.Difficulty;
                    break;
            }
        }

        foreach (var (member, earned) in feats)
            Grant(member, earned);
    }

    private Member? MemberOf(int heroId) => _members.Values.FirstOrDefault(member => member.Hero.Id == heroId);

    private void Grant(Member member, LiveFeats feats)
    {
        if (_trophies is null || feats.IsEmpty)
            return;

        foreach (var trophy in _trophies(member.Session.AccountId, feats))
            member.Trophies.Add(new TrophyResponse(trophy.Id, trophy.Name, trophy.Description));
    }

    private void FlushCoins(Member member)
    {
        if (member.PendingCoins <= 0)
            return;

        var coins = member.PendingCoins;
        member.PendingCoins = 0;
        Grant(member, new LiveFeats(member.Session.ClassId) { Coins = coins });
    }

    private static IReadOnlyList<TrophyResponse>? TakeTrophies(Member member)
    {
        if (member.Trophies.Count == 0)
            return null;

        var taken = member.Trophies.ToList();
        member.Trophies.Clear();
        return taken;
    }

    private List<(string, LiveSnapshot)> MoveTo(int number, bool viaStairs)
    {
        foreach (var member in _members.Values)
        {
            Grant(member, new LiveFeats(member.Session.ClassId)
            {
                CleanFloor = viaStairs && !member.HurtOnFloor,
                FloorReached = number,
                Falls = member.Session.Falls
            });
            FlushCoins(member);
            member.HurtOnFloor = !viaStairs;
        }

        _world.SetGraves(_floor.FloorNumber, LiveArena.GravesOf(_floor));
        var next = LiveArena.OpenFloor(_world, number, _random, _floor.Tick);
        next.Reinforce(_members.Count);
        _world.Reach(number);

        foreach (var member in _members.Values)
        {
            _floor.RemoveHero(member.Hero.Id);
            member.Hero = LiveArena.Enter(next, member.Hero.Id, member.Session);

            if (member.AwayUntil is not null)
                next.SetAway(member.Hero.Id, true);
            member.SavedHeroVersion = member.Hero.Version;
            member.SavedAt = next.Time;
            _store.Save(member.Session);
        }

        _store.Save(_world);
        _worldSavedAt = next.Time;
        _floor = next;
        _itemsVersion = next.ItemsVersion;
        _partyVersion++;

        return _members.Values
            .Where(member => member.AwayUntil is null)
            .Select(member =>
            {
                var welcome = Welcome(member);
                return (member.ConnectionId, MonstersFor(member, LiveMapper.Snapshot(_floor, [])) with { Hero = welcome.Hero, Party = welcome.Party, NewFloor = welcome, Trophies = TakeTrophies(member) });
            })
            .ToList();
    }

    private void SaveChangedHeroes()
    {
        foreach (var member in _members.Values)
        {
            if (member.Hero.Version == member.SavedHeroVersion || _floor.Time - member.SavedAt < SaveSeconds)
                continue;

            member.SavedHeroVersion = member.Hero.Version;
            member.SavedAt = _floor.Time;
            FlushCoins(member);
            _store.Save(member.Session);
        }

        if (_members.Count > 0 && _floor.Time - _worldSavedAt >= WorldSaveSeconds)
        {
            _worldSavedAt = _floor.Time;
            _store.Save(_world);
        }
    }

    private LiveSnapshot MonstersFor(Member member, LiveSnapshot shared)
    {
        if (member.SentMonsters is not { } sent || _floor.Time - member.MonstersSentAt >= KeyframeSeconds)
        {
            member.SentMonsters = shared.Monsters.ToDictionary(monster => monster.Id);
            member.MonstersSentAt = _floor.Time;
            return shared with { Full = true };
        }

        var changed = shared.Monsters.Where(monster => !sent.TryGetValue(monster.Id, out var known) || known != monster).ToList();
        var present = shared.Monsters.Select(monster => monster.Id).ToHashSet();
        var gone = sent.Keys.Where(id => !present.Contains(id)).ToList();

        foreach (var monster in changed)
            sent[monster.Id] = monster;
        foreach (var id in gone)
            sent.Remove(id);

        return shared with { Monsters = changed, Gone = gone.Count == 0 ? null : gone };
    }

    private LiveWelcome Welcome(Member member)
    {
        member.SentMonsters = null;
        member.SentHeroVersion = member.Hero.Version;
        member.HeroSentAt = _floor.Time;
        member.SentPartyVersion = _partyVersion;
        return LiveMapper.Welcome(_world, member.Session, _floor, member.Hero, Code, member.ConnectionId == HostConnectionId, IsShared, Party(), member.ResumeToken);
    }

    private IReadOnlyList<LivePartyMember> Party() =>
        _members.Values
            .Select(member => new LivePartyMember(
                member.Hero.Id,
                member.Hero.Name,
                member.Hero.ClassId,
                member.ConnectionId == HostConnectionId,
                GameMapper.ToResponse(member.Session).Player.Equipment))
            .OrderBy(member => member.HeroId)
            .ToList();
}

public sealed record LiveAct(LiveFloor Floor, LiveHero Hero, Game Game)
{
    public Player Player => Hero.Player;
}
