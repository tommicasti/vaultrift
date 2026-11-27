namespace Vaultrift.Infrastructure.Cosmos;

internal static class DocumentTypes
{
    public const string Game = "game";
    public const string Account = "account";
    public const string Name = "name";
    public const string Owner = "owner";
    public const string Trophy = "trophy";
    public const string Counter = "counter";
    public const string Vault = "vault";
    public const string Key = "key";
}

internal sealed record GameDocument(
    string Id,
    string Pk,
    string Type,
    string Code,
    string ClassId,
    string HeroName,
    int Floor,
    int Level,
    string Data,
    SaveStats Stats,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

internal sealed record AccountDocument(
    string Id,
    string Pk,
    string Type,
    string UserName,
    string NormalizedName,
    string PasswordHash,
    string? Email,
    DateTimeOffset CreatedAt);

internal sealed record NameDocument(string Id, string Pk, string Type, string AccountId);

internal sealed record OwnerDocument(string Id, string Pk, string Type, string GameId, string AccountId, DateTimeOffset LinkedAt);

internal sealed record TrophyDocument(string Id, string Pk, string Type, string AccountId, string TrophyId, DateTimeOffset UnlockedAt);

internal sealed record CounterDocument(string Id, string Pk, string Type, string AccountId, string Name, long Value);

internal sealed record VaultDocument(
    string Id,
    string Pk,
    string Type,
    string AccountId,
    string Kind,
    string EntryId,
    string Data,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

internal static class DocumentKeys
{
    public static string Of(Guid id) => id.ToString("D");

    public static string NameKey(string normalizedName) => "name:" + normalizedName;

    public static string OwnerKey(Guid gameId) => "game:" + Of(gameId);

    public static string TrophyKey(string trophyId) => "trophy:" + trophyId;

    public static string CounterKey(string name) => "counter:" + name;

    public static string VaultKey(string kind, Guid id) => kind + ":" + Of(id);
}

internal sealed record KeyDocument(string Id, string Pk, string Type, string Xml);
