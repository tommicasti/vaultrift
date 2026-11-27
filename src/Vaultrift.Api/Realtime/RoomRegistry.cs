using System.Collections.Concurrent;
using System.Security.Cryptography;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Realtime;

public enum RoomOpening
{
    Opened,
    Full,
    WorldBusy
}

public sealed class RoomRegistry(int maxRooms, IVaultSaver store, Func<Guid, LiveFeats, IReadOnlyList<TrophyDefinition>>? trophies = null)
{
    public const int DefaultMaxRooms = 12;
    public const int CodeLength = 6;

    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly ConcurrentDictionary<string, Room> _byConnection = new();
    private readonly ConcurrentDictionary<Guid, string> _heroes = new();
    private readonly ConcurrentDictionary<string, Guid> _heroByConnection = new();
    private readonly Lock _changing = new();

    public int Count => _rooms.Count;

    public IReadOnlyList<Room> All => _rooms.Values.ToList();

    public (RoomOpening Result, Room? Room) Create(string connectionId, WorldSession world, bool isShared, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(world);

        lock (_changing)
        {
            Leave(connectionId);

            if (_rooms.Values.Any(room => room.WorldId == world.Id))
                return (RoomOpening.WorldBusy, null);

            if (_rooms.Count >= maxRooms)
                return (RoomOpening.Full, null);

            var code = NewCode();
            var room = new Room(code, connectionId, world, isShared, store, now, trophies);
            _rooms[code] = room;
            _byConnection[connectionId] = room;
            return (RoomOpening.Opened, room);
        }
    }

    public bool IsWorldOpen(Guid worldId) => _rooms.Values.Any(room => room.WorldId == worldId);

    public Room? Find(string code) =>
        Normalize(code) is { } normalized ? _rooms.GetValueOrDefault(normalized) : null;

    public Room? RoomOf(string connectionId) => _byConnection.GetValueOrDefault(connectionId);

    public bool IsInWorld(Guid heroId) => _heroes.ContainsKey(heroId);

    public bool TryClaim(string connectionId, Guid heroId)
    {
        lock (_changing)
        {
            if (_heroes.TryGetValue(heroId, out var owner))
                return owner == connectionId;

            Release(connectionId);
            _heroes[heroId] = connectionId;
            _heroByConnection[connectionId] = heroId;
            return true;
        }
    }

    public void Release(string connectionId)
    {
        lock (_changing)
        {
            if (_heroByConnection.TryRemove(connectionId, out var heroId))
                _heroes.TryRemove(heroId, out _);
        }
    }

    public void Track(string connectionId, Room room)
    {
        lock (_changing)
        {
            if (_byConnection.TryGetValue(connectionId, out var current) && current != room)
                Leave(connectionId);

            _byConnection[connectionId] = room;
        }
    }

    public IReadOnlyList<string> Leave(string connectionId)
    {
        lock (_changing)
        {
            if (!_byConnection.TryRemove(connectionId, out var room))
                return [];

            if (room.HostConnectionId != connectionId)
            {
                room.Leave(connectionId);
                Release(connectionId);
                return [];
            }

            return Close(room).Where(connection => connection != connectionId).ToList();
        }
    }

    public bool Park(string connectionId, DateTimeOffset now)
    {
        lock (_changing)
        {
            if (!_byConnection.TryRemove(connectionId, out var room))
                return false;

            if (room.Park(connectionId, now))
                return true;

            Release(connectionId);
            return false;
        }
    }

    public LiveWelcome? Resume(string token, Guid accountId, string connectionId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        lock (_changing)
        {
            if (_rooms.Values.FirstOrDefault(room => room.HasToken(token)) is not { } room)
                return null;

            if (_byConnection.TryGetValue(connectionId, out var current) && current != room)
                Leave(connectionId);

            if (room.Resume(token, accountId, connectionId, now) is not { } resumed)
                return null;

            _byConnection.TryRemove(resumed.Previous, out _);
            _byConnection[connectionId] = room;

            if (_heroByConnection.TryRemove(resumed.Previous, out var heroId))
            {
                Release(connectionId);
                _heroes[heroId] = connectionId;
                _heroByConnection[connectionId] = heroId;
            }

            return resumed.Welcome;
        }
    }

    public IReadOnlyList<string> ExpireAway(DateTimeOffset now)
    {
        lock (_changing)
        {
            var notify = new List<string>();

            foreach (var room in _rooms.Values.ToList())
            {
                foreach (var connection in room.ExpiredAway(now))
                {
                    if (connection == room.HostConnectionId)
                    {
                        notify.AddRange(Close(room));
                        break;
                    }

                    room.Leave(connection);
                    Release(connection);
                }
            }

            return notify;
        }
    }

    public IReadOnlyList<string> Close(Room room)
    {
        lock (_changing)
        {
            _rooms.TryRemove(room.Code, out _);
            var connections = room.Connections;
            room.Close();

            foreach (var connection in connections)
            {
                _byConnection.TryRemove(connection, out _);
                Release(connection);
            }

            return connections;
        }
    }

    public static string? Normalize(string? code)
    {
        var letters = new string((code ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return letters.Length == CodeLength && letters.All(SaveCode.Alphabet.Contains) ? letters : null;
    }

    private string NewCode()
    {
        while (true)
        {
            var code = new string(Enumerable.Range(0, CodeLength).Select(_ => SaveCode.Alphabet[RandomNumberGenerator.GetInt32(SaveCode.Alphabet.Length)]).ToArray());

            if (!_rooms.ContainsKey(code))
                return code;
        }
    }
}
