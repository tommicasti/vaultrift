using System.Security.Cryptography;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;

namespace Vaultrift.Infrastructure.Persistence;

public sealed class GameSession
{
    private static readonly TimeSpan PauseThreshold = TimeSpan.FromMinutes(3);

    private readonly SeededRandom _random;
    private readonly VaultFloors _floors;
    private DateTimeOffset _lastPlayed;

    private GameSession(Guid id, string code, string classId, SeededRandom random, VaultFloors floors, Game game, long playSeconds = 0)
    {
        Id = id;
        Code = code;
        ClassId = classId;
        _random = random;
        _floors = floors;
        Game = game;
        LastUsed = DateTimeOffset.UtcNow;
        _lastPlayed = LastUsed;
        PlaySeconds = playSeconds;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string ClassId { get; }

    public string? BiomeOf(int floor) =>
        floor >= 1 && floor <= _floors.Biomes.Count ? _floors.Biomes[floor - 1].Id
        : floor == VaultFloors.FinalFloor ? RiftArena.BiomeId
        : null;

    public Game Game { get; }

    public int FloorCount => _floors.Count;

    public Floor CreateFloor(int number) => _floors.Create(number);

    public DateTimeOffset LastUsed { get; private set; }

    public long PlaySeconds { get; private set; }

    public static GameSession Start(Guid id, string code, ClassDefinition playerClass, string? playerName, ulong? seed = null, Difficulty difficulty = Difficulty.Normal)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        var classId = playerClass.Class.Id;
        var random = new SeededRandom(seed ?? NewSeed());
        var floors = new VaultFloors(random, classId, difficulty);
        var game = VaultDungeon.CreateGame(random, floors, playerClass, playerName);

        return new GameSession(id, code, classId, random, floors, game);
    }

    public static GameSession Resume(Guid id, string code, SaveFile save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var random = new SeededRandom(save.RandomState);
        var floors = new VaultFloors(random, save.ClassId, save.Biomes, save.Game.Difficulty);
        var game = VaultDungeon.RestoreGame(save.Game, random, floors);

        return new GameSession(id, code, save.ClassId, random, floors, game, save.PlaySeconds);
    }

    public SaveFile ToSave() =>
        new(
            SaveFile.CurrentVersion,
            _random.State,
            ClassId,
            _floors.Biomes.Select(biome => biome.Id).ToList(),
            Game.Capture(),
            PlaySeconds);

    public SaveRecord ToRecord() =>
        new(Id, Code, ClassId, Game.Player.Name, Game.FloorNumber, Game.Player.Level, ToSave().ToJson(), DateTimeOffset.UtcNow);

    public void Touch() => LastUsed = DateTimeOffset.UtcNow;

    public void RecordPlay(DateTimeOffset now)
    {
        var gap = now - _lastPlayed;

        if (gap > TimeSpan.Zero && gap <= PauseThreshold)
            PlaySeconds += (long)gap.TotalSeconds;

        if (now > _lastPlayed)
            _lastPlayed = now;
    }

    private static ulong NewSeed() => BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong)));
}
