using System.Security.Cryptography;

namespace Vaultrift.Infrastructure.Persistence;

public static class SaveCode
{
    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int GroupLength = 4;
    public const int Length = GroupLength * 2;

    public static string Generate()
    {
        Span<char> letters = stackalloc char[Length];

        for (var index = 0; index < Length; index++)
            letters[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return Format(letters);
    }

    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var letters = input
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        if (letters.Length != Length || letters.Any(letter => !Alphabet.Contains(letter)))
            return null;

        return Format(letters);
    }

    private static string Format(ReadOnlySpan<char> letters) =>
        $"{letters[..GroupLength]}-{letters[GroupLength..]}";
}
