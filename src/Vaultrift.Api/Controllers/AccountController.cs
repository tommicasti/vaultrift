using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Hosting;
using Vaultrift.Infrastructure.Accounts;

namespace Vaultrift.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private static readonly Dictionary<RegisterStatus, string> Problems = new()
    {
        [RegisterStatus.InvalidName] = $"The name must be {AccountRules.MinNameLength}–{AccountRules.MaxNameLength} letters, digits, dots, dashes or underscores.",
        [RegisterStatus.InvalidPassword] = $"The password must be at least {AccountRules.MinPasswordLength} characters.",
        [RegisterStatus.InvalidEmail] = "That email address does not look valid.",
        [RegisterStatus.NameTaken] = "That name is already taken.",
    };

    private readonly AccountService _accounts;
    private readonly TrophyService _trophies;

    public AccountController(AccountService accounts, TrophyService trophies)
    {
        _accounts = accounts;
        _trophies = trophies;
    }

    [HttpGet]
    public async Task<ActionResult<AccountResponse>> Me()
    {
        var account = User.AccountId() is { } id ? _accounts.Find(id) : null;

        if (account is not null)
            return Describe(account);

        if (User.Identity?.IsAuthenticated == true)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return NoContent();
    }

    [HttpPost("register")]
    [EnableRateLimiting(AccountSetup.SignInPolicy)]
    public async Task<ActionResult<AccountResponse>> Register(RegisterRequest request)
    {
        var result = _accounts.Register(request.UserName, request.Password, request.Email);

        if (result.Account is null)
            return Problem(Problems[result.Status], statusCode: result.Status == RegisterStatus.NameTaken ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);

        await SignIn(result.Account);
        return Describe(result.Account, Claim(result.Account, request.Codes));
    }

    [HttpPost("login")]
    [EnableRateLimiting(AccountSetup.SignInPolicy)]
    public async Task<ActionResult<AccountResponse>> Login(LoginRequest request)
    {
        var account = _accounts.SignIn(request.UserName, request.Password);

        if (account is null)
            return Problem("Wrong name or password.", statusCode: StatusCodes.Status401Unauthorized);

        await SignIn(account);
        return Describe(account, Claim(account, request.Codes));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost("claim")]
    public ActionResult<AccountResponse> ClaimGames(ClaimRequest request)
    {
        var account = User.AccountId() is { } id ? _accounts.Find(id) : null;

        if (account is null)
            return Unauthorized();

        return Describe(account, Claim(account, request.Codes));
    }

    private int Claim(Account account, IReadOnlyList<string>? codes) =>
        codes is null || codes.Count == 0 ? 0 : _accounts.Claim(account.Id, codes);

    private AccountResponse Describe(Account account, int claimed = 0)
    {
        var heroes = _accounts.HeroesOf(account.Id);
        _trophies.CatchUp(account.Id, heroes);

        return new(
            account.UserName,
            account.Email is not null,
            heroes
                .Select(hero => new HeroSummaryResponse(
                    hero.Id,
                    hero.Code,
                    hero.Name,
                    hero.ClassId,
                    hero.Floor,
                    hero.Level,
                    hero.Difficulty.ToString(),
                    hero.IsLost,
                    hero.IsConquered,
                    hero.PlaySeconds,
                    hero.UpdatedAt))
                .ToList(),
            _trophies
                .StatusOf(account.Id)
                .Select(status => new TrophyResponse(
                    status.Trophy.Id,
                    status.Trophy.Name,
                    status.Trophy.Description,
                    status.UnlockedAt,
                    status.Progress,
                    status.Trophy.Goal))
                .ToList(),
            claimed);
    }

    private Task SignIn(Account account)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()), new Claim(ClaimTypes.Name, account.UserName)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}
