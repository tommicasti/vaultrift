namespace Vaultrift.Infrastructure.Persistence;

public sealed record SaveRecord(
    Guid Id,
    string Code,
    string ClassId,
    string HeroName,
    int Floor,
    int Level,
    string Data,
    DateTimeOffset UpdatedAt);
