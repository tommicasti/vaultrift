using Vaultrift.Engine.Gameplay;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Tests.Accounts;

public class AccountServiceTests
{
    private readonly MemoryAccountRepository _accounts = new();
    private readonly MemorySaveRepository _saves = new();

    private AccountService Service() => new(_accounts, _saves, iterations: 1000);

    private GameSession SavedGame(string name = "Tommaso")
    {
        var session = GameSession.Start(Guid.NewGuid(), SaveCode.Generate(), ClassCatalog.Find("knight")!, name, 7, Difficulty.Hard);
        _saves.Upsert(session.ToRecord());
        return session;
    }

    [Fact]
    public void ANewPlayer_CanRegisterAndSignIn()
    {
        var service = Service();

        var result = service.Register("Tommaso", "password123", null);

        Assert.Equal(RegisterStatus.Created, result.Status);
        Assert.Equal(result.Account!.Id, service.SignIn("tommaso", "password123")?.Id);
    }

    [Fact]
    public void AWrongPassword_OrAnUnknownName_DoesNotSignIn()
    {
        var service = Service();
        service.Register("Tommaso", "password123", null);

        Assert.Null(service.SignIn("Tommaso", "password124"));
        Assert.Null(service.SignIn("Nobody", "password123"));
    }

    [Fact]
    public void Names_AreUniqueIgnoringCase()
    {
        var service = Service();
        service.Register("Tommaso", "password123", null);

        Assert.Equal(RegisterStatus.NameTaken, service.Register("TOMMASO", "password456", null).Status);
    }

    [Theory]
    [InlineData("ab", "password123", null, RegisterStatus.InvalidName)]
    [InlineData("has space", "password123", null, RegisterStatus.InvalidName)]
    [InlineData("Tommaso", "short", null, RegisterStatus.InvalidPassword)]
    [InlineData("Tommaso", "password123", "not-an-email", RegisterStatus.InvalidEmail)]
    [InlineData("Tommaso", "password123", "me@localhost", RegisterStatus.InvalidEmail)]
    public void InvalidDetails_AreRejected(string name, string password, string? email, RegisterStatus expected)
    {
        Assert.Equal(expected, Service().Register(name, password, email).Status);
    }

    [Fact]
    public void TheEmail_IsOptionalAndKeptWhenGiven()
    {
        var service = Service();

        Assert.Null(service.Register("NoMail", "password123", "  ").Account!.Email);
        Assert.Equal("me@example.com", service.Register("WithMail", "password123", " me@example.com ").Account!.Email);
    }

    [Fact]
    public void ClaimedGames_ShowUpAsTheAccountsHeroes()
    {
        var service = Service();
        var account = service.Register("Tommaso", "password123", null).Account!;
        var game = SavedGame();

        Assert.Equal(1, service.Claim(account.Id, [game.Code.ToLowerInvariant().Replace("-", ""), "ZZZZ-ZZZZ", "nonsense"]));

        var hero = Assert.Single(service.HeroesOf(account.Id));
        Assert.Equal(game.Code, hero.Code);
        Assert.Equal("Tommaso", hero.Name);
        Assert.Equal(Difficulty.Hard, hero.Difficulty);
    }

    [Fact]
    public void AGameOwnedBySomeoneElse_CannotBeClaimed()
    {
        var service = Service();
        var first = service.Register("First", "password123", null).Account!;
        var second = service.Register("Second", "password123", null).Account!;
        var game = SavedGame();

        service.Claim(first.Id, [game.Code]);

        Assert.Equal(0, service.Claim(second.Id, [game.Code]));
        Assert.Empty(service.HeroesOf(second.Id));
        Assert.Equal(LinkResult.AlreadyYours, service.Link(first.Id, game.Id));
    }

    [Fact]
    public void AForgottenHero_LeavesTheAccount_AndOnlyItsOwnerCanForgetIt()
    {
        var service = Service();
        var owner = service.Register("Owner", "password123", null).Account!;
        var stranger = service.Register("Stranger", "password123", null).Account!;
        var game = SavedGame();
        service.Claim(owner.Id, [game.Code]);

        Assert.False(service.Unlink(stranger.Id, game.Id));
        Assert.True(service.Owns(owner.Id, game.Id));

        Assert.True(service.Unlink(owner.Id, game.Id));
        Assert.False(service.Owns(owner.Id, game.Id));
        Assert.Empty(service.HeroesOf(owner.Id));
    }
}
