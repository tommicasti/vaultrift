using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Persistence;

public class SaveCodeTests
{
    [Fact]
    public void AGeneratedCode_HasTwoGroupsOfFourSafeLetters()
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var code = SaveCode.Generate();

            Assert.Equal(9, code.Length);
            Assert.Equal('-', code[4]);
            Assert.All(code.Replace("-", ""), letter => Assert.Contains(letter, SaveCode.Alphabet));
        }
    }

    [Theory]
    [InlineData("abcd-efgh", "ABCD-EFGH")]
    [InlineData(" abcd efgh ", "ABCD-EFGH")]
    [InlineData("ABCDEFGH", "ABCD-EFGH")]
    [InlineData("k7m2-p9qx", "K7M2-P9QX")]
    public void Normalize_AcceptsWhatPlayersType(string input, string expected) =>
        Assert.Equal(expected, SaveCode.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABCD-EFG")]
    [InlineData("ABCD-EFGHJ")]
    [InlineData("ABCD-EFG0")]
    [InlineData("ABCD-EFGI")]
    [InlineData("../../etc")]
    public void Normalize_RejectsAnythingElse(string? input) =>
        Assert.Null(SaveCode.Normalize(input));
}
