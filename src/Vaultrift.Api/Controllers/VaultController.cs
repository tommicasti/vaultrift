using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Hosting;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Api.Mapping;
using Vaultrift.Api.Realtime;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Realtime;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vault")]
public sealed class VaultController(VaultService vault, RoomRegistry rooms) : ControllerBase
{
    [HttpGet]
    public ActionResult<VaultResponse> Get() => User.AccountId() is { } accountId ? Describe(accountId) : Unauthorized();

    [HttpPost("heroes")]
    public ActionResult<VaultHeroResponse> CreateHero(CreateHeroRequest request)
    {
        if (User.AccountId() is not { } accountId)
            return Unauthorized();

        if (ClassCatalog.Find(request.ClassId) is not { } playerClass)
        {
            ModelState.AddModelError(nameof(request.ClassId), $"Unknown class '{request.ClassId}'.");
            return ValidationProblem(ModelState);
        }

        var hero = vault.CreateHero(accountId, playerClass, request.Name);

        if (hero is null)
            return Problem($"An account can keep up to {VaultService.MaxHeroes} heroes.", statusCode: StatusCodes.Status409Conflict);

        return Describe(accountId).Heroes.First(card => card.Id == hero.Id);
    }

    [HttpDelete("heroes/{id:guid}")]
    public ActionResult<VaultResponse> DeleteHero(Guid id)
    {
        if (User.AccountId() is not { } accountId)
            return Unauthorized();

        if (rooms.IsInWorld(id))
            return Problem("That hero is in a world right now.", statusCode: StatusCodes.Status409Conflict);

        return vault.DeleteHero(accountId, id) ? Describe(accountId) : NotFound();
    }

    [HttpPost("worlds")]
    public ActionResult<VaultWorldResponse> CreateWorld(CreateWorldRequest request)
    {
        if (User.AccountId() is not { } accountId)
            return Unauthorized();

        var classId = request.ClassId is { } wanted && ClassCatalog.Find(wanted) is not null ? wanted : ClassCatalog.All[0].Class.Id;
        var world = vault.CreateWorld(accountId, request.Difficulty, classId);

        if (world is null)
            return Problem($"An account can keep up to {VaultService.MaxWorlds} worlds.", statusCode: StatusCodes.Status409Conflict);

        return Describe(accountId).Worlds.First(card => card.Id == world.Id);
    }

    [HttpDelete("worlds/{id:guid}")]
    public ActionResult<VaultResponse> DeleteWorld(Guid id)
    {
        if (User.AccountId() is not { } accountId)
            return Unauthorized();

        if (rooms.IsWorldOpen(id))
            return Problem("That world is open right now.", statusCode: StatusCodes.Status409Conflict);

        return vault.DeleteWorld(accountId, id) ? Describe(accountId) : NotFound();
    }

    [HttpGet("deposit/{heroId:guid}")]
    public ActionResult<DepositResponse> Deposit(Guid heroId)
    {
        if (User.AccountId() is not { } accountId)
            return Unauthorized();

        return DescribeDeposit(accountId, heroId) is { } deposit ? deposit : NotFound();
    }

    [HttpPost("deposit/{heroId:guid}/store/{entryId:int}")]
    public ActionResult<DepositResponse> Store(Guid heroId, int entryId) =>
        Move(heroId, accountId => vault.Store(accountId, heroId, entryId));

    [HttpPost("deposit/{heroId:guid}/take/{index:int}")]
    public ActionResult<DepositResponse> Take(Guid heroId, int index) =>
        Move(heroId, accountId => vault.Take(accountId, heroId, index));

    [HttpPost("deposit/{heroId:guid}/store-equipped/{slot}")]
    public ActionResult<DepositResponse> StoreEquipped(Guid heroId, EquipmentSlot slot) =>
        Move(heroId, accountId => vault.StoreEquipped(accountId, heroId, slot));

    [HttpPost("deposit/{heroId:guid}/equip/{entryId:int}")]
    public ActionResult<DepositResponse> Equip(Guid heroId, int entryId) =>
        Change(heroId, accountId => vault.Equip(accountId, heroId, entryId) switch
        {
            null => "That hero is gone.",
            EquipResult.Equipped => null,
            EquipResult.WrongClass => "That is not for this hero's class.",
            EquipResult.BackpackFull => "The backpack is full: make room for what the hero is wearing now.",
            EquipResult.NotEquippable => "That cannot be worn.",
            _ => "That cannot be equipped."
        });

    [HttpPost("deposit/{heroId:guid}/unequip/{slot}")]
    public ActionResult<DepositResponse> Unequip(Guid heroId, EquipmentSlot slot) =>
        Change(heroId, accountId => vault.Unequip(accountId, heroId, slot) switch
        {
            null => "That hero is gone.",
            UnequipResult.Unequipped => null,
            UnequipResult.BackpackFull => "The backpack is full.",
            _ => "Nothing is worn there."
        });

    private ActionResult<DepositResponse> Change(Guid heroId, Func<Guid, string?> change)
    {
        if (User.AccountId() is not { } accountId)
            return Unauthorized();

        if (rooms.IsInWorld(heroId))
            return Problem("That hero is in a world right now.", statusCode: StatusCodes.Status409Conflict);

        return change(accountId) is { } refusal
            ? Problem(refusal, statusCode: StatusCodes.Status409Conflict)
            : DescribeDeposit(accountId, heroId) is { } deposit ? deposit : NotFound();
    }

    private ActionResult<DepositResponse> Move(Guid heroId, Func<Guid, DepositResult> move)
    {
        if (User.AccountId() is not { } accountId)
            return Unauthorized();

        if (rooms.IsInWorld(heroId))
            return Problem("That hero is in a world right now.", statusCode: StatusCodes.Status409Conflict);

        return move(accountId) switch
        {
            DepositResult.Done => DescribeDeposit(accountId, heroId) is { } deposit ? deposit : NotFound(),
            DepositResult.DepositFull => Problem($"The deposit is full ({VaultService.DepositCapacity} items).", statusCode: StatusCodes.Status409Conflict),
            DepositResult.BackpackFull => Problem("The hero's backpack is full.", statusCode: StatusCodes.Status409Conflict),
            _ => NotFound()
        };
    }

    private DepositResponse? DescribeDeposit(Guid accountId, Guid heroId)
    {
        if (vault.FindHero(accountId, heroId) is not { } hero)
            return null;

        return new DepositResponse(
            VaultService.DepositCapacity,
            vault.Deposit(accountId).Select((stored, index) => new DepositSlotResponse(index, GameMapper.ToItemResponse(stored.Item), stored.Quantity)).ToList(),
            hero.Player.Inventory.Entries.Select(entry => new InventoryEntryResponse(entry.Id, GameMapper.ToItemResponse(entry.Item), entry.Quantity, entry.Mark, 0)).ToList(),
            hero.Player.Inventory.Capacity,
            hero.Player.Equipment
                .OrderBy(equipped => equipped.Key)
                .Select(equipped => new EquippedItemResponse(equipped.Key, equipped.Value.Id, GameMapper.ToItemResponse(equipped.Value.Item), equipped.Value.Mark, 0))
                .ToList());
    }

    private VaultResponse Describe(Guid accountId)
    {
        var contents = vault.Contents(accountId);

        return new VaultResponse(
            contents.Heroes
                .Select(hero => new VaultHeroResponse(
                    hero.Id,
                    hero.Name,
                    hero.ClassId,
                    hero.Level,
                    hero.Played,
                    hero.PlaySeconds,
                    rooms.IsInWorld(hero.Id),
                    hero.UpdatedAt,
                    AccountController.GearOf(hero.Gear)))
                .ToList(),
            contents.Worlds
                .Select(world => new VaultWorldResponse(
                    world.Id,
                    world.Difficulty,
                    world.Floor,
                    world.DeepestFloor,
                    LiveArena.LastFloor,
                    world.Biome,
                    world.PlaySeconds,
                    rooms.IsWorldOpen(world.Id),
                    world.CreatedAt,
                    world.UpdatedAt,
                    world.IsConquered,
                    world.GraveOwners ?? []))
                .ToList());
    }
}
