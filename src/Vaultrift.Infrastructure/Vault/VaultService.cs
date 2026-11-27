using System.Collections.Concurrent;
using System.Security.Cryptography;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Persistence;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Realtime;

namespace Vaultrift.Infrastructure.Vault;

public sealed record HeroCard(
    Guid Id,
    string Name,
    string ClassId,
    int Level,
    IReadOnlyList<Difficulty> Played,
    long PlaySeconds,
    IReadOnlyDictionary<EquipmentSlot, ItemSnapshot> Gear,
    DateTimeOffset UpdatedAt);

public sealed record WorldCard(
    Guid Id,
    Difficulty Difficulty,
    int Floor,
    int DeepestFloor,
    string? Biome,
    long PlaySeconds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsConquered = false,
    IReadOnlyList<Guid>? GraveOwners = null);

public sealed record VaultContents(IReadOnlyList<HeroCard> Heroes, IReadOnlyList<WorldCard> Worlds);

public sealed class VaultService : IVaultSaver
{
    public const int MaxHeroes = 24;
    public const int MaxWorlds = 24;
    public const int DepositCapacity = 24;

    private readonly IVaultRepository _vault;
    private readonly IAccountRepository _accounts;
    private readonly ISaveRepository _saves;
    private readonly TimeProvider _time;
    private readonly Action<Guid, Exception>? _onUnreadable;
    private readonly ConcurrentDictionary<Guid, bool> _migrated = new();
    private readonly ConcurrentDictionary<Guid, object> _depositGates = new();

    public VaultService(
        IVaultRepository vault,
        IAccountRepository accounts,
        ISaveRepository saves,
        TimeProvider? time = null,
        Action<Guid, Exception>? onUnreadable = null)
    {
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(saves);

        _vault = vault;
        _accounts = accounts;
        _saves = saves;
        _time = time ?? TimeProvider.System;
        _onUnreadable = onUnreadable;
    }

    public VaultContents Contents(Guid accountId)
    {
        Migrate(accountId);
        return Peek(accountId);
    }

    public VaultContents Peek(Guid accountId)
    {
        var entries = _vault.List(accountId);

        return new VaultContents(
            entries.Where(entry => entry.Kind == VaultKind.Hero).Select(HeroCardOf).OfType<HeroCard>().ToList(),
            entries.Where(entry => entry.Kind == VaultKind.World).Select(WorldCardOf).OfType<WorldCard>().ToList());
    }

    public HeroSession? CreateHero(Guid accountId, ClassDefinition playerClass, string? name)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        if (Count(accountId, VaultKind.Hero) >= MaxHeroes)
            return null;

        var hero = HeroSession.Create(Guid.NewGuid(), accountId, playerClass, name, _time.GetUtcNow());
        Save(hero);
        return hero;
    }

    public WorldSession? CreateWorld(Guid accountId, Difficulty difficulty, string classId)
    {
        if (Count(accountId, VaultKind.World) >= MaxWorlds)
            return null;

        var world = WorldSession.Start(Guid.NewGuid(), accountId, classId, difficulty, _time.GetUtcNow());
        Save(world);
        return world;
    }

    public HeroSession? FindHero(Guid accountId, Guid heroId)
    {
        Migrate(accountId);
        return PeekHero(accountId, heroId);
    }

    public HeroSession? PeekHero(Guid accountId, Guid heroId) =>
        _vault.Find(accountId, VaultKind.Hero, heroId) is { } entry ? Load(entry, HeroSession.Restore) : null;

    public WorldSession? FindWorld(Guid accountId, Guid worldId)
    {
        Migrate(accountId);
        return PeekWorld(accountId, worldId);
    }

    public WorldSession? PeekWorld(Guid accountId, Guid worldId) =>
        _vault.Find(accountId, VaultKind.World, worldId) is { } entry ? Load(entry, WorldSession.Restore) : null;

    public bool DeleteHero(Guid accountId, Guid heroId)
    {
        var removed = _vault.Delete(accountId, VaultKind.Hero, heroId);

        if (removed)
            _accounts.Unlink(accountId, heroId);

        return removed;
    }

    public bool DeleteWorld(Guid accountId, Guid worldId) => _vault.Delete(accountId, VaultKind.World, worldId);

    public void Save(HeroSession hero)
    {
        ArgumentNullException.ThrowIfNull(hero);

        var now = _time.GetUtcNow();
        hero.RecordPlay(now);
        _vault.Upsert(hero.ToEntry(now));
    }

    public void Save(WorldSession world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var now = _time.GetUtcNow();
        world.RecordPlay(now);
        _vault.Upsert(world.ToEntry(now));
    }

    public IReadOnlyList<(ItemInstance Item, int Quantity)> Deposit(Guid accountId) =>
        ReadDeposit(accountId).Items.Select(stored => (Restore(stored.Item), stored.Quantity)).ToList();

    public DepositResult Store(Guid accountId, Guid heroId, int entryId)
    {
        lock (DepositGate(accountId))
        {
            if (FindHero(accountId, heroId) is not { } hero)
                return DepositResult.HeroNotFound;

            var deposit = ReadDeposit(accountId);

            if (deposit.Items.Count >= DepositCapacity)
                return DepositResult.DepositFull;

            if (hero.Player.Withdraw(entryId) is not { } entry)
                return DepositResult.ItemNotFound;

            var now = _time.GetUtcNow();
            WriteDeposit(accountId, deposit with { Items = [.. deposit.Items, new DepositItem(new ItemSnapshot(entry.Item.Id, entry.Item.Element), entry.Quantity)] }, now);
            _vault.Upsert(hero.ToEntry(now));
            return DepositResult.Done;
        }
    }

    public DepositResult StoreEquipped(Guid accountId, Guid heroId, EquipmentSlot slot)
    {
        lock (DepositGate(accountId))
        {
            if (FindHero(accountId, heroId) is not { } hero)
                return DepositResult.HeroNotFound;

            var deposit = ReadDeposit(accountId);

            if (deposit.Items.Count >= DepositCapacity)
                return DepositResult.DepositFull;

            if (hero.Player.TakeOff(slot) is not { } entry)
                return DepositResult.ItemNotFound;

            var now = _time.GetUtcNow();
            WriteDeposit(accountId, deposit with { Items = [.. deposit.Items, new DepositItem(new ItemSnapshot(entry.Item.Id, entry.Item.Element), entry.Quantity)] }, now);
            _vault.Upsert(hero.ToEntry(now));
            return DepositResult.Done;
        }
    }

    public EquipResult? Equip(Guid accountId, Guid heroId, int entryId)
    {
        lock (DepositGate(accountId))
        {
            if (FindHero(accountId, heroId) is not { } hero)
                return null;

            var result = hero.Player.Equip(entryId);

            if (result == EquipResult.Equipped)
                _vault.Upsert(hero.ToEntry(_time.GetUtcNow()));

            return result;
        }
    }

    public UnequipResult? Unequip(Guid accountId, Guid heroId, EquipmentSlot slot)
    {
        lock (DepositGate(accountId))
        {
            if (FindHero(accountId, heroId) is not { } hero)
                return null;

            var result = hero.Player.Unequip(slot);

            if (result == UnequipResult.Unequipped)
                _vault.Upsert(hero.ToEntry(_time.GetUtcNow()));

            return result;
        }
    }

    public DepositResult Take(Guid accountId, Guid heroId, int index)
    {
        lock (DepositGate(accountId))
        {
            if (FindHero(accountId, heroId) is not { } hero)
                return DepositResult.HeroNotFound;

            var deposit = ReadDeposit(accountId);

            if (index < 0 || index >= deposit.Items.Count)
                return DepositResult.ItemNotFound;

            var stored = deposit.Items[index];

            if (!hero.Player.Receive(Restore(stored.Item), stored.Quantity))
                return DepositResult.BackpackFull;

            var now = _time.GetUtcNow();
            WriteDeposit(accountId, deposit with { Items = deposit.Items.Where((_, position) => position != index).ToList() }, now);
            _vault.Upsert(hero.ToEntry(now));
            return DepositResult.Done;
        }
    }

    public void Keep(Guid accountId, ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (DepositGate(accountId))
        {
            var deposit = ReadDeposit(accountId);
            WriteDeposit(accountId, deposit with { Items = [.. deposit.Items, new DepositItem(new ItemSnapshot(item.Id, item.Element), 1)] }, _time.GetUtcNow());
        }
    }

    private object DepositGate(Guid accountId) => _depositGates.GetOrAdd(accountId, _ => new object());

    private DepositSave ReadDeposit(Guid accountId)
    {
        if (_vault.Find(accountId, VaultKind.Deposit, accountId) is not { } entry)
            return DepositSave.Empty;

        try
        {
            return DepositSave.FromJson(entry.Data);
        }
        catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException)
        {
            _onUnreadable?.Invoke(entry.Id, error);
            return DepositSave.Empty;
        }
    }

    private void WriteDeposit(Guid accountId, DepositSave deposit, DateTimeOffset now)
    {
        var created = _vault.Find(accountId, VaultKind.Deposit, accountId)?.CreatedAt ?? now;
        _vault.Upsert(new VaultEntry(accountId, accountId, VaultKind.Deposit, deposit.ToJson(), created, now));
    }

    private static ItemInstance Restore(ItemSnapshot item) => new(VaultContent.Instance.Item(item.Id), item.Element);

    public void Lose(WorldSession world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _vault.Delete(world.AccountId, VaultKind.World, world.Id);
    }

    public static Guid WorldIdFor(Guid gameId)
    {
        var bytes = MD5.HashData(gameId.ToByteArray().Concat("world"u8.ToArray()).ToArray());
        return new Guid(bytes);
    }

    private int Count(Guid accountId, VaultKind kind) => _vault.List(accountId).Count(entry => entry.Kind == kind);

    private void Migrate(Guid accountId)
    {
        if (_migrated.ContainsKey(accountId))
            return;

        var known = _vault.List(accountId).Where(entry => entry.Kind == VaultKind.Hero).Select(entry => entry.Id).ToHashSet();

        foreach (var gameId in _accounts.GamesOf(accountId).Where(id => !known.Contains(id)))
        {
            try
            {
                MigrateGame(accountId, gameId);
            }
            catch (Exception error) when (IsUnreadable(error))
            {
                _onUnreadable?.Invoke(gameId, error);
            }
        }

        _migrated[accountId] = true;
    }

    private static bool IsUnreadable(Exception error) =>
        error is InvalidDataException or System.Text.Json.JsonException or ArgumentException or KeyNotFoundException or InvalidOperationException or InvalidCastException;

    private void MigrateGame(Guid accountId, Guid gameId)
    {
        if (_saves.FindById(gameId) is not { } record)
            return;

        var save = SaveFile.FromJson(record.Data);
        var hero = HeroSession.FromSnapshot(gameId, accountId, save.Game.Player, [save.Game.Difficulty], save.PlaySeconds, record.UpdatedAt);
        hero.Player.Rest();

        if (save.Game.IsLost)
        {
            hero.LoseEverything();
            _vault.Upsert(hero.ToEntry(record.UpdatedAt));
            return;
        }

        var game = save.Game;
        var floor = Math.Clamp(game.FloorNumber, 1, LiveArena.LastFloor);
        var deepest = Math.Clamp(Math.Max(floor, game.DeepestFloor ?? floor), 1, LiveArena.LastFloor);
        var graves = game.Grave is { } grave
            ? new[] { new GraveSave(gameId, hero.Name, grave.Floor > 0 ? grave.Floor : game.FloorNumber, grave.Items) }
            : [];
        var world = WorldSession.FromSave(
            WorldIdFor(gameId),
            accountId,
            new WorldSave(WorldSave.CurrentVersion, save.RandomState, save.ClassId, game.Difficulty, save.Biomes, floor, deepest, save.PlaySeconds,
                game.ClearedFloors ?? [], game.TotemUses, game.IsConquered, graves),
            record.UpdatedAt);

        _vault.Upsert(hero.ToEntry(record.UpdatedAt));
        _vault.Upsert(world.ToEntry(record.UpdatedAt));
    }

    private static T? Load<T>(VaultEntry entry, Func<VaultEntry, T> restore) where T : class
    {
        try
        {
            return restore(entry);
        }
        catch (Exception error) when (IsUnreadable(error))
        {
            return null;
        }
    }

    private static HeroCard? HeroCardOf(VaultEntry entry)
    {
        try
        {
            var save = HeroSave.FromJson(entry.Data);
            var player = save.Player;
            return new HeroCard(
                entry.Id,
                player.Name,
                player.ClassId ?? string.Empty,
                player.Level,
                save.Played,
                save.PlaySeconds,
                player.Equipment.ToDictionary(pair => pair.Key, pair => pair.Value.Item),
                entry.UpdatedAt);
        }
        catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            return null;
        }
    }

    private static WorldCard? WorldCardOf(VaultEntry entry)
    {
        try
        {
            var save = WorldSave.FromJson(entry.Data);
            var biome = save.Floor >= 1 && save.Floor <= save.Biomes.Count ? save.Biomes[save.Floor - 1]
                : save.Floor == VaultFloors.FinalFloor ? RiftArena.BiomeId
                : null;
            return new WorldCard(entry.Id, save.Difficulty, save.Floor, save.DeepestFloor, biome, save.PlaySeconds, entry.CreatedAt, entry.UpdatedAt,
                save.IsConquered, (save.Graves ?? []).Select(grave => grave.Owner).Distinct().ToList());
        }
        catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            return null;
        }
    }
}
