namespace Vaultrift.Infrastructure.Accounts;

public sealed record Account(Guid Id, string UserName, string PasswordHash, string? Email, DateTimeOffset CreatedAt)
{
    public string NormalizedName => AccountRules.NormalizeName(UserName);
}
