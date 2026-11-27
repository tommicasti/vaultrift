using Vaultrift.Infrastructure.Accounts;

namespace Vaultrift.Api.Hosting;

public sealed class Admins(string? names)
{
    private readonly HashSet<string> _names = (names ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(AccountRules.NormalizeName)
        .ToHashSet();

    public bool Includes(Account? account) => account is not null && _names.Contains(AccountRules.NormalizeName(account.UserName));
}
