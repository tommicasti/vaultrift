using System.Collections.Concurrent;
using System.Security.Cryptography;
using Vaultrift.Engine.Randomness;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Realtime;

public sealed class RoomRegistry(int maxRooms)
{
    public const int DefaultMaxRooms = 12;
    public const int CodeLength = 6;

    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly ConcurrentDictionary<string, Room> _byConnection = new();
    private readonly Lock _changing = new();

    public int Count => _rooms.Count;

    public IReadOnlyList<Room> All => _rooms.Values.ToList();

    public Room? Create(string connectionId, GameSession session, IRandom random, DateTimeOffset now)
    {
        lock (_changing)
        {
            Leave(connectionId);

            if (_rooms.Count >= maxRooms)
                return null;

            var code = NewCode();
            var room = new Room(code, connectionId, session, random, now);
            _rooms[code] = room;
            _byConnection[connectionId] = room;
            return room;
        }
    }

    public Room? Find(string code) =>
        Normalize(code) is { } normalized ? _rooms.GetValueOrDefault(normalized) : null;

    public Room? RoomOf(string connectionId) => _byConnection.GetValueOrDefault(connectionId);

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
                return [];
            }

            return Close(room).Where(connection => connection != connectionId).ToList();
        }
    }

    public IReadOnlyList<string> Close(Room room)
    {
        lock (_changing)
        {
            _rooms.TryRemove(room.Code, out _);
            var connections = room.Connections;

            foreach (var connection in connections)
                _byConnection.TryRemove(connection, out _);

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
