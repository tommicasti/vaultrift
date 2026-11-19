using Microsoft.AspNetCore.Mvc;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Mapping;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Levels;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameStore _store;
    private readonly IRandom _random;

    public GamesController(IGameStore store, IRandom random)
    {
        _store = store;
        _random = random;
    }

    [HttpPost]
    public ActionResult<GameStateResponse> Create(CreateGameRequest request)
    {
        var playerClass = ClassCatalog.Find(request.ClassId);

        if (playerClass is null)
        {
            ModelState.AddModelError(nameof(request.ClassId), $"Unknown class '{request.ClassId}'.");
            return ValidationProblem(ModelState);
        }

        var game = VaultDungeon.CreateGame(_random, playerClass, request.Name);
        var id = _store.Add(game);

        return CreatedAtAction(nameof(Get), new { id }, GameMapper.ToResponse(id, game));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GameStateResponse> Get(Guid id)
    {
        var game = _store.Find(id);

        if (game is null)
            return NotFound();

        lock (game)
        {
            return GameMapper.ToResponse(id, game);
        }
    }

    [HttpPost("{id:guid}/move")]
    public ActionResult<TurnResponse> Move(Guid id, MoveRequest request) =>
        Play(id, game => game.MovePlayer(request.Direction));

    [HttpPost("{id:guid}/attack")]
    public ActionResult<TurnResponse> Attack(Guid id) =>
        Play(id, game => game.Attack());

    [HttpPost("{id:guid}/equip")]
    public ActionResult<TurnResponse> Equip(Guid id, EntryRequest request) =>
        Play(id, game => game.Equip(request.EntryId, request.Hand));

    [HttpPost("{id:guid}/unequip")]
    public ActionResult<TurnResponse> Unequip(Guid id, UnequipRequest request) =>
        Play(id, game => game.Unequip(request.Slot));

    [HttpPost("{id:guid}/stats")]
    public ActionResult<TurnResponse> SpendPoint(Guid id, SpendPointRequest request) =>
        Play(id, game => game.SpendPoint(request.Stat));

    [HttpPost("{id:guid}/ability")]
    public ActionResult<TurnResponse> UseAbility(Guid id) =>
        Play(id, game => game.UseAbility());

    [HttpPost("{id:guid}/discard")]
    public ActionResult<TurnResponse> Discard(Guid id, EntryRequest request) =>
        Play(id, game => game.Discard(request.EntryId));

    [HttpPost("{id:guid}/discard-junk")]
    public ActionResult<TurnResponse> DiscardJunk(Guid id) =>
        Play(id, game => game.DiscardJunk());

    [HttpPost("{id:guid}/mark")]
    public ActionResult<TurnResponse> Mark(Guid id, MarkRequest request) =>
        Play(id, game => game.Mark(request.EntryId, request.Mark));

    [HttpPost("{id:guid}/use")]
    public ActionResult<TurnResponse> Use(Guid id, EntryRequest request) =>
        Play(id, game => game.UseItem(request.EntryId));

    [HttpPost("{id:guid}/quick-slot")]
    public ActionResult<TurnResponse> AssignQuickSlot(Guid id, QuickSlotRequest request) =>
        Play(id, game => game.AssignQuickSlot(request.EntryId ?? 0, request.Slot));

    [HttpPost("{id:guid}/quick-slot/use")]
    public ActionResult<TurnResponse> UseQuickSlot(Guid id, QuickSlotRequest request) =>
        Play(id, game => game.UseQuickSlot(request.Slot));

    [HttpPost("{id:guid}/spell")]
    public ActionResult<TurnResponse> SelectSpell(Guid id, SpellRequest request) =>
        Play(id, game => game.SelectSpell(request.Index));

    [HttpPost("{id:guid}/learn")]
    public ActionResult<TurnResponse> LearnSpell(Guid id, EntryRequest request) =>
        Play(id, game => game.LearnSpell(request.EntryId));

    private ActionResult<TurnResponse> Play(Guid id, Func<Game, TurnResult> action)
    {
        var game = _store.Find(id);

        if (game is null)
            return NotFound();

        lock (game)
        {
            var result = action(game);
            return GameMapper.ToResponse(id, game, result);
        }
    }
}