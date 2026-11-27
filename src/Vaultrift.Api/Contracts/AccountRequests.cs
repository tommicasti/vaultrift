using System.ComponentModel.DataAnnotations;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Infrastructure.Accounts;

namespace Vaultrift.Api.Contracts;

public sealed class RegisterRequest
{
    [Required, StringLength(AccountRules.MaxNameLength)]
    public required string UserName { get; init; }

    [Required, StringLength(AccountRules.MaxPasswordLength)]
    public required string Password { get; init; }

    [StringLength(AccountRules.MaxEmailLength)]
    public string? Email { get; init; }

    public IReadOnlyList<string>? Codes { get; init; }
}

public sealed class LoginRequest
{
    [Required, StringLength(AccountRules.MaxNameLength)]
    public required string UserName { get; init; }

    [Required, StringLength(AccountRules.MaxPasswordLength)]
    public required string Password { get; init; }

    public IReadOnlyList<string>? Codes { get; init; }
}

public sealed class ClaimRequest
{
    [Required, MaxLength(AccountService.MaxClaimsPerRequest)]
    public required IReadOnlyList<string> Codes { get; init; }
}

public sealed record AccountResponse(string UserName, bool HasEmail, IReadOnlyList<HeroSummaryResponse> Heroes, IReadOnlyList<TrophyResponse> Trophies, int Claimed = 0);

public sealed record HeroSummaryResponse(
    Guid Id,
    string Code,
    string Name,
    string ClassId,
    int Floor,
    int Level,
    string Difficulty,
    bool IsLost,
    bool IsConquered,
    long PlaySeconds,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<HeroGearResponse> Gear);

public sealed record HeroGearResponse(EquipmentSlot Slot, ItemResponse Item);
