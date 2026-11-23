using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Vaultrift.Infrastructure.Accounts;

public static partial class AccountRules
{
    public const int MinNameLength = 3;
    public const int MaxNameLength = 20;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxEmailLength = 254;

    public static string NormalizeName(string userName) => userName.Trim().ToUpperInvariant();

    public static bool IsValidName(string? userName) =>
        userName is not null && NamePattern().IsMatch(userName.Trim());

    public static bool IsValidPassword(string? password) =>
        password is not null && password.Length is >= MinPasswordLength and <= MaxPasswordLength;

    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var trimmed = email.Trim();

        if (trimmed.Length > MaxEmailLength || !MailAddress.TryCreate(trimmed, out var address) || address.Address != trimmed)
            return null;

        var domain = address.Host;
        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.') ? trimmed : null;
    }

    [GeneratedRegex("^[A-Za-z0-9_.-]{3,20}$")]
    private static partial Regex NamePattern();
}
