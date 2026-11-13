using Microsoft.AspNetCore.Mvc;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Mapping;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Randomness;
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
    public ActionResult<GameStateResponse> Create()
    {
        var game = StarterLevel.CreateGame(_random);
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
    public ActionResult<TurnResponse> Equip(Guid id, EquipRequest request) =>
    Play(id, game => game.Equip(request.EntryId));

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