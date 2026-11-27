using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Hosting;
using Vaultrift.Api.Mapping;
using Vaultrift.Api.Realtime;
using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Cosmos;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminController(AccountService accounts, TrophyService trophies, VaultService vault, RoomRegistry rooms, Admins admins, TimeProvider time) : ControllerBase
{
    public static readonly IReadOnlySet<Rarity> TreasureRarities = new HashSet<Rarity> { Rarity.Legendary, Rarity.Exotic, Rarity.Platinum };

    [HttpGet("players")]
    public ActionResult<PlayersReport> Players()
    {
        if (!admins.Includes(User.AccountId() is { } id ? accounts.Find(id) : null))
            return NotFound();

        var players = accounts.All().Select(Describe).OrderByDescending(player => player.LastSeen ?? player.JoinedAt).ToList();
        return new PlayersReport(time.GetUtcNow(), players);
    }

    private PlayerReport Describe(Account account)
    {
        var contents = vault.Peek(account.Id);
        var heroes = contents.Heroes.Select(card => DescribeHero(account.Id, card)).OfType<ReportHero>().ToList();
        var worlds = contents.Worlds.Select(card => DescribeWorld(account.Id, card)).ToList();
        var turnHeroes = accounts.HeroesOf(account.Id).Select(DescribeTurnHero).ToList();
        var deposit = vault.Deposit(account.Id);
        var seen = heroes.Select(hero => hero.UpdatedAt)
            .Concat(worlds.Select(world => world.UpdatedAt))
            .Concat(turnHeroes.Select(hero => hero.UpdatedAt))
            .ToList();

        return new PlayerReport(
            account.UserName,
            account.CreatedAt,
            seen.Count == 0 ? null : seen.Max(),
            heroes.Any(hero => hero.Online),
            heroes.Sum(hero => hero.PlaySeconds) + turnHeroes.Sum(hero => hero.PlaySeconds),
            trophies.StatusOf(account.Id)
                .Where(status => status.UnlockedAt is not null)
                .OrderBy(status => status.UnlockedAt)
                .Select(status => new ReportTrophy(status.Trophy.Name, status.Trophy.Description, status.UnlockedAt!.Value))
                .ToList(),
            heroes,
            worlds,
            new ReportDeposit(
                deposit.Sum(stored => stored.Quantity),
                deposit.Where(stored => TreasureRarities.Contains(stored.Item.Rarity))
                    .Select(stored => Item(stored.Item, stored.Quantity))
                    .ToList()),
            turnHeroes);
    }

    private ReportHero? DescribeHero(Guid accountId, HeroCard card)
    {
        if (vault.PeekHero(accountId, card.Id) is not { } hero)
            return null;

        return new ReportHero(
            hero.Name,
            ClassName(hero.ClassId),
            hero.Player.Level,
            hero.Player.Coins,
            hero.PlaySeconds,
            hero.Falls,
            rooms.IsInWorld(hero.Id),
            card.UpdatedAt,
            hero.Player.Equipment
                .OrderBy(equipped => equipped.Key)
                .Select(equipped => Item(equipped.Value.Item, 1, equipped.Key.ToString()))
                .ToList(),
            hero.Player.Inventory.Entries.Sum(entry => entry.Quantity));
    }

    private ReportWorld DescribeWorld(Guid accountId, WorldCard card) =>
        new(
            vault.PeekWorld(accountId, card.Id) is { } world ? ClassName(world.ClassId) : string.Empty,
            card.Difficulty.ToString(),
            card.Biome is { } biome ? BiomeCatalog.All.FirstOrDefault(candidate => candidate.Id == biome)?.Name ?? biome : null,
            card.Floor,
            card.DeepestFloor,
            card.IsConquered,
            rooms.IsWorldOpen(card.Id),
            card.PlaySeconds,
            card.UpdatedAt);

    private ReportTurnHero DescribeTurnHero(HeroSummary hero)
    {
        var stats = accounts.SaveOf(hero.Id) is { } save ? SaveStats.From(save.Data) : SaveStats.Empty;

        return new ReportTurnHero(
            hero.Name,
            ClassName(hero.ClassId),
            hero.Level,
            hero.Floor,
            hero.Difficulty.ToString(),
            stats.Turns,
            stats.Coins,
            stats.Deaths,
            hero.PlaySeconds,
            hero.IsLost,
            hero.IsConquered,
            hero.UpdatedAt);
    }

    private static ReportItem Item(ItemInstance item, int quantity, string? slot = null)
    {
        var view = GameMapper.ToItemResponse(item);
        return new ReportItem(view.Name, view.Rarity, quantity, slot);
    }

    private static string ClassName(string classId) => ClassCatalog.Find(classId)?.Class.Name ?? classId;
}
