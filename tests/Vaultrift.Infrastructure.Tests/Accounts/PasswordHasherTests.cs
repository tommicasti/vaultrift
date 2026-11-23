using Vaultrift.Infrastructure.Accounts;

namespace Vaultrift.Infrastructure.Tests.Accounts;

public class PasswordHasherTests
{
    [Fact]
    public void TheRightPassword_Verifies()
    {
        var hash = PasswordHasher.Hash("correct horse", iterations: 1000);

        Assert.True(PasswordHasher.Verify("correct horse", hash));
        Assert.False(PasswordHasher.Verify("Correct horse", hash));
    }

    [Fact]
    public void TheSamePassword_GetsADifferentSaltEachTime()
    {
        Assert.NotEqual(PasswordHasher.Hash("same password", 1000), PasswordHasher.Hash("same password", 1000));
    }

    [Fact]
    public void TheHash_NeverContainsThePassword()
    {
        Assert.DoesNotContain("secret-password", PasswordHasher.Hash("secret-password", 1000));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("pbkdf2-sha256$x$abc$def")]
    [InlineData("pbkdf2-sha256$1000$***$***")]
    public void BrokenHashes_NeverVerify(string stored)
    {
        Assert.False(PasswordHasher.Verify("anything", stored));
    }
}
