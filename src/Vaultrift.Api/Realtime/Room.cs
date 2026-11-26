using Vaultrift.Api.Contracts;
using Vaultrift.Api.Mapping;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Realtime;

namespace Vaultrift.Api.Realtime;

public sealed class Room
{
    public const double HeroStateSeconds = 0.2;

    private readonly Lock _gate = new();
    private readonly LiveFloor _floor;
    private readonly Dictionary<string, Member> _members = [];
    private int _nextHeroId = LiveArena.HeroId + 1;
    private int _itemsVersion;
    private int _partyVersion;

    private sealed class Member(string connectionId, GameSession session, LiveHero hero)
    {
        public string ConnectionId { get; } = connectionId;

        public GameSession Session { get; } = session;

        public LiveHero Hero { get; } = hero;

        public int SentHeroVersion { get; set; } = -1;

        public double HeroSentAt { get; set; } = double.NegativeInfinity;

        public int SentPartyVersion { get; set; } = -1;
    }

    public Room(string code, string hostConnectionId, GameSession hostSession, IRandom random, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostConnectionId);
        ArgumentNullException.ThrowIfNull(hostSession);
        ArgumentNullException.ThrowIfNull(random);

        Code = code;
        HostConnectionId = hostConnectionId;
        _floor = LiveArena.Open(hostSession, random);
        _members[hostConnectionId] = new Member(hostConnectionId, hostSession, _floor.Heroes[0]);
        LastInputAt = now;
    }

    public string Code { get; }

    public string HostConnectionId { get; }

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

    public LiveWelcome? Enter(string connectionId, GameSession session, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(session);

        lock (_gate)
        {
            if (!_members.ContainsKey(connectionId))
            {
                if (_members.Count >= LiveRules.MaxHeroes)
                    return null;

                var hero = LiveArena.Enter(_floor, _nextHeroId++, session);
                _members[connectionId] = new Member(connectionId, session, hero);
                _partyVersion++;
            }

            LastInputAt = now;
            return Welcome(_members[connectionId]);
        }
    }

    public void Leave(string connectionId)
    {
        lock (_gate)
        {
            if (!_members.Remove(connectionId, out var member))
                return;

            _floor.RemoveHero(member.Hero.Id);
            _partyVersion++;
        }
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

            for (var tick = 0; tick < ticks; tick++)
                events.AddRange(_floor.Step());

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
                LiveHeroState? hero = null;

                if (member.Hero.Version != member.SentHeroVersion && _floor.Time - member.HeroSentAt >= HeroStateSeconds)
                {
                    member.SentHeroVersion = member.Hero.Version;
                    member.HeroSentAt = _floor.Time;
                    hero = LiveMapper.HeroState(member.Session, _floor, member.Hero);
                }

                IReadOnlyList<LivePartyMember>? memberParty = null;

                if (member.SentPartyVersion != _partyVersion)
                {
                    member.SentPartyVersion = _partyVersion;
                    memberParty = party ??= Party();
                }

                sends.Add((member.ConnectionId, shared with { Hero = hero, Party = memberParty }));
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
            var response = GameMapper.ToResponse(member.Session, result);
            member.SentHeroVersion = member.Hero.Version;
            _partyVersion++;
            return new LiveActionResponse(response.Events, LiveMapper.HeroState(member.Session, _floor, member.Hero));
        }
    }

    public StatsPreviewResponse? Preview(string connectionId, int entryId, EquipmentSlot? hand)
    {
        lock (_gate)
        {
            if (!_members.TryGetValue(connectionId, out var member))
                return null;

            var preview = member.Hero.Player.PreviewEquip(entryId, hand, VaultContent.Instance);

            return new StatsPreviewResponse(
                preview.Result,
                preview.MaxHealth,
                preview.MaxMana,
                preview.Attack,
                preview.Defense,
                preview.Evasion,
                preview.Critical,
                preview.Block,
                preview.Resistance);
        }
    }

    private LiveWelcome Welcome(Member member)
    {
        member.SentHeroVersion = member.Hero.Version;
        member.HeroSentAt = _floor.Time;
        member.SentPartyVersion = _partyVersion;
        return LiveMapper.Welcome(member.Session, _floor, member.Hero, Code, member.ConnectionId == HostConnectionId, Party());
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
