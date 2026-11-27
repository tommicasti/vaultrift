using System.Collections.Concurrent;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Persistence;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Accounts;

public enum RegisterStatus
{
    Created,
    InvalidName,
    InvalidPassword,
    InvalidEmail,
    NameTaken
}

public sealed record RegisterResult(RegisterStatus Status, Account? Account = null);

public sealed record HeroSummary(
    Guid Id,
    string Code,
    string Name,
    string ClassId,
    int Floor,
    int Level,
    Difficulty Difficulty,
    bool IsLost,
    bool IsConquered,
    long PlaySeconds,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<EquipmentSlot, ItemSnapshot>? Gear = null);

public sealed class AccountService
{
    public const int MaxClaimsPerRequest = 30;

    private static readonly Lazy<string> DecoyHash = new(() => PasswordHasher.Hash(Guid.NewGuid().ToString()));

    private readonly IAccountRepository _accounts;
    private readonly ISaveRepository _saves;
    private readonly Func<DateTimeOffset> _clock;
    private readonly int _iterations;
    private readonly ConcurrentDictionary<Guid, Guid?> _owners = new();

    public AccountService(IAccountRepository accounts, ISaveRepository saves, Func<DateTimeOffset>? clock = null, int iterations = PasswordHasher.DefaultIterations)
    {
        _accounts = accounts;
        _saves = saves;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _iterations = iterations;
    }

    public RegisterResult Register(string? userName, string? password, string? email)
    {
        if (!AccountRules.IsValidName(userName))
            return new RegisterResult(RegisterStatus.InvalidName);

        if (!AccountRules.IsValidPassword(password))
            return new RegisterResult(RegisterStatus.InvalidPassword);

        var normalizedEmail = AccountRules.NormalizeEmail(email);
        if (!string.IsNullOrWhiteSpace(email) && normalizedEmail is null)
            return new RegisterResult(RegisterStatus.InvalidEmail);

        if (_accounts.FindByName(userName!) is not null)
            return new RegisterResult(RegisterStatus.NameTaken);

        var account = new Account(Guid.NewGuid(), userName!.Trim(), PasswordHasher.Hash(password!, _iterations), normalizedEmail, _clock());

        return _accounts.TryCreate(account)
            ? new RegisterResult(RegisterStatus.Created, account)
            : new RegisterResult(RegisterStatus.NameTaken);
    }

    public Account? SignIn(string? userName, string? password)
    {
        if (string.IsNullOrWhiteSpace(userName) || password is null || password.Length > AccountRules.MaxPasswordLength)
            return null;

        var account = AccountRules.IsValidName(userName) ? _accounts.FindByName(userName) : null;

        if (account is null)
        {
            PasswordHasher.Verify(password, DecoyHash.Value);
            return null;
        }

        return PasswordHasher.Verify(password, account.PasswordHash) ? account : null;
    }

    public Account? Find(Guid id) => _accounts.FindById(id);

    public IReadOnlyList<Account> All() => _accounts.All();

    public SaveRecord? SaveOf(Guid gameId) => _saves.FindById(gameId);

    public LinkResult Link(Guid accountId, Guid gameId)
    {
        var result = _accounts.Link(accountId, gameId);
        _owners.TryRemove(gameId, out _);
        return result;
    }

    public Guid? OwnerOf(Guid gameId) => _owners.GetOrAdd(gameId, _accounts.OwnerOf);

    public bool Owns(Guid accountId, Guid gameId) => OwnerOf(gameId) == accountId;

    public bool Unlink(Guid accountId, Guid gameId)
    {
        var removed = _accounts.Unlink(accountId, gameId);
        _owners.TryRemove(gameId, out _);
        return removed;
    }

    public int Claim(Guid accountId, IEnumerable<string> codes)
    {
        var linked = 0;

        foreach (var code in codes.Take(MaxClaimsPerRequest))
        {
            var normalized = SaveCode.Normalize(code);
            var record = normalized is null ? null : _saves.FindByCode(normalized);

            if (record is not null && Link(accountId, record.Id) == LinkResult.Linked)
                linked++;
        }

        return linked;
    }

    public IReadOnlyList<HeroSummary> HeroesOf(Guid accountId) =>
        _accounts
            .GamesOf(accountId)
            .Select(_saves.FindById)
            .OfType<SaveRecord>()
            .Select(Summarize)
            .OfType<HeroSummary>()
            .OrderByDescending(hero => hero.UpdatedAt)
            .ToList();

    private static HeroSummary? Summarize(SaveRecord record)
    {
        try
        {
            var save = SaveFile.FromJson(record.Data);
            return new HeroSummary(
                record.Id,
                record.Code,
                record.HeroName,
                record.ClassId,
                record.Floor,
                record.Level,
                save.Game.Difficulty,
                save.Game.IsLost,
                save.Game.IsConquered,
                save.PlaySeconds,
                record.UpdatedAt,
                save.Game.Player.Equipment.ToDictionary(pair => pair.Key, pair => pair.Value.Item));
        }
        catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            return null;
        }
    }
}
