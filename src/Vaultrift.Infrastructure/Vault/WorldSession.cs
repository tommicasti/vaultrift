using System.Security.Cryptography;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Levels;

namespace Vaultrift.Infrastructure.Vault;

public sealed class WorldSession
{
    private static readonly TimeSpan PauseThreshold = TimeSpan.FromMinutes(3);

    private readonly VaultFloors _floors;
    private readonly SortedSet<int> _cleared = [];
    private readonly List<GraveSave> _graves = [];
    private DateTimeOffset _lastPlayed = DateTimeOffset.MinValue;

    private WorldSession(Guid id, Guid accountId, string classId, Difficulty difficulty, SeededRandom random, VaultFloors floors, int floor, int deepest, long playSeconds, DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        ClassId = classId;
        Difficulty = difficulty;
        Random = random;
        _floors = floors;
        Floor = floor;
        DeepestFloor = deepest;
        PlaySeconds = playSeconds;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid AccountId { get; }

    public string ClassId { get; }

    public Difficulty Difficulty { get; }

    public SeededRandom Random { get; }

    public int Floor { get; private set; }

    public int DeepestFloor { get; private set; }

    public long PlaySeconds { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public int TotemUses { get; private set; }

    public bool IsConquered { get; private set; }

    public IReadOnlyCollection<int> ClearedFloors => _cleared;

    public IReadOnlyList<GraveSave> Graves => _graves;

    public bool IsCleared(int floor) => _cleared.Contains(floor);

    public void MarkCleared(int floor) => _cleared.Add(floor);

    public void UseTotem() => TotemUses++;

    public void Conquer() => IsConquered = true;

    public IReadOnlyList<GraveSave> GravesOn(int floor) => _graves.Where(grave => grave.Floor == floor).ToList();

    public void SetGraves(int floor, IEnumerable<GraveSave> graves)
    {
        ArgumentNullException.ThrowIfNull(graves);

        var placed = graves.ToList();
        _graves.RemoveAll(grave => grave.Floor == floor || placed.Any(fresh => fresh.Owner == grave.Owner));
        _graves.AddRange(placed);
    }

    public void ForgetGravesOf(Guid owner) => _graves.RemoveAll(grave => grave.Owner == owner);

    public int FloorCount => _floors.Count;

    public string? BiomeOf(int floor) =>
        floor >= 1 && floor <= _floors.Biomes.Count ? _floors.Biomes[floor - 1].Id
        : floor == VaultFloors.FinalFloor ? RiftArena.BiomeId
        : null;

    public Engine.Gameplay.Floor CreateFloor(int number) => _floors.Create(number);

    public static WorldSession Start(Guid id, Guid accountId, string classId, Difficulty difficulty, DateTimeOffset now, ulong? seed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);

        var random = new SeededRandom(seed ?? BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong))));
        return new WorldSession(id, accountId, classId, difficulty, random, new VaultFloors(random, classId, difficulty), 1, 1, 0, now);
    }

    public static WorldSession FromSave(Guid id, Guid accountId, WorldSave save, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(save);

        var random = new SeededRandom(save.RandomState);
        var floors = new VaultFloors(random, save.ClassId, save.Biomes, save.Difficulty);
        var floor = Math.Clamp(save.Floor, 1, floors.Count);
        var world = new WorldSession(id, accountId, save.ClassId, save.Difficulty, random, floors, floor, Math.Max(floor, save.DeepestFloor), save.PlaySeconds, createdAt)
        {
            TotemUses = save.TotemUses,
            IsConquered = save.IsConquered
        };

        foreach (var cleared in save.ClearedFloors ?? [])
            world._cleared.Add(cleared);

        world._graves.AddRange(save.Graves ?? []);
        return world;
    }

    public static WorldSession Restore(VaultEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Kind != VaultKind.World)
            throw new ArgumentException("The entry is not a world.", nameof(entry));

        return FromSave(entry.Id, entry.AccountId, WorldSave.FromJson(entry.Data), entry.CreatedAt);
    }

    public WorldSave ToSave() =>
        new(WorldSave.CurrentVersion, Random.State, ClassId, Difficulty, [.. _floors.Biomes.Select(biome => biome.Id)], Floor, DeepestFloor, PlaySeconds,
            [.. _cleared], TotemUses, IsConquered, [.. _graves]);

    public VaultEntry ToEntry(DateTimeOffset now) => new(Id, AccountId, VaultKind.World, ToSave().ToJson(), CreatedAt, now);

    public void Reach(int floor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(floor, 1);
        Floor = floor;
        DeepestFloor = Math.Max(DeepestFloor, floor);
    }

    public void RecordPlay(DateTimeOffset now)
    {
        var gap = now - _lastPlayed;

        if (gap > TimeSpan.Zero && gap <= PauseThreshold)
            PlaySeconds += (long)gap.TotalSeconds;

        if (now > _lastPlayed)
            _lastPlayed = now;
    }
}
