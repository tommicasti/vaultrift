using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Persistence;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Vault;

public sealed class HeroSession
{
    private static readonly TimeSpan PauseThreshold = TimeSpan.FromMinutes(3);
    private static readonly string[] Pocket = ["###", "#.#", "###"];
    private static readonly Position PocketTile = new(1, 1);

    private readonly HashSet<Difficulty> _played;
    private DateTimeOffset _lastPlayed;

    private HeroSession(Guid id, Guid accountId, PlayerSnapshot player, IEnumerable<Difficulty> played, long playSeconds, int falls, DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        CreatedAt = createdAt;
        PlaySeconds = playSeconds;
        Falls = falls;
        _played = [.. played];
        _lastPlayed = DateTimeOffset.MinValue;
        Game = Wrap(player);
    }

    public Guid Id { get; }

    public Guid AccountId { get; }

    public DateTimeOffset CreatedAt { get; }

    public Game Game { get; private set; }

    public Player Player => Game.Player;

    public string ClassId => Player.Class?.Id ?? string.Empty;

    public string Name => Player.Name;

    public long PlaySeconds { get; private set; }

    public int Falls { get; private set; }

    public IReadOnlyCollection<Difficulty> Played => _played;

    public static HeroSession Create(Guid id, Guid accountId, ClassDefinition playerClass, string? name, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(playerClass);
        return new HeroSession(id, accountId, VaultDungeon.CreateHero(playerClass, name).Capture(), [], 0, 0, now);
    }

    public static HeroSession FromSnapshot(Guid id, Guid accountId, PlayerSnapshot player, IEnumerable<Difficulty> played, long playSeconds, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(played);
        return new HeroSession(id, accountId, player, played, playSeconds, 0, createdAt);
    }

    public static HeroSession Restore(VaultEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Kind != VaultKind.Hero)
            throw new ArgumentException("The entry is not a hero.", nameof(entry));

        var save = HeroSave.FromJson(entry.Data);
        return new HeroSession(entry.Id, entry.AccountId, save.Player, save.Played, save.PlaySeconds, save.Falls, entry.CreatedAt);
    }

    public VaultEntry ToEntry(DateTimeOffset now) =>
        new(Id, AccountId, VaultKind.Hero, new HeroSave(HeroSave.CurrentVersion, Player.Capture(), [.. _played.Order()], PlaySeconds, Falls).ToJson(), CreatedAt, now);

    public void MarkPlayed(Difficulty difficulty) => _played.Add(difficulty);

    public void RecordFall() => Falls++;

    public void RecordPlay(DateTimeOffset now)
    {
        var gap = now - _lastPlayed;

        if (gap > TimeSpan.Zero && gap <= PauseThreshold)
            PlaySeconds += (long)gap.TotalSeconds;

        if (now > _lastPlayed)
            _lastPlayed = now;
    }

    public void LoseEverything()
    {
        var kept = Player.Capture();
        var playerClass = ClassCatalog.Find(ClassId) ?? throw new InvalidOperationException($"Unknown class {ClassId}.");
        var fresh = VaultDungeon.CreateHero(playerClass, Name).Capture();

        Game = Wrap(fresh with
        {
            Level = kept.Level,
            Experience = kept.Experience,
            UnspentPoints = kept.UnspentPoints,
            SpentPoints = kept.SpentPoints
        });
        Player.Rest();
    }

    private static Game Wrap(PlayerSnapshot snapshot)
    {
        var player = Player.Restore(snapshot with { Position = PocketTile }, VaultContent.Instance);
        return new Game(DungeonMap.Parse(Pocket), player, [], new SeededRandom(1));
    }
}
