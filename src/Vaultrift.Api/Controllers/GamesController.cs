using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Hosting;
using Vaultrift.Api.Mapping;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/games")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameStore _store;
    private readonly AccountService _accounts;
    private readonly TrophyService _trophies;
    private readonly ILogger<GamesController> _logger;

    public GamesController(IGameStore store, AccountService accounts, TrophyService trophies, ILogger<GamesController> logger)
    {
        _store = store;
        _accounts = accounts;
        _trophies = trophies;
        _logger = logger;
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

        if (User.AccountId() is null)
            return Unauthorized();

        var session = _store.Create(playerClass, request.Name, request.Difficulty);

        _trophies.StartFloor(session.Id, session.Game.FloorNumber);

        var accountId = User.AccountId()!.Value;
        _accounts.Link(accountId, session.Id);
        GrantDragonShard(accountId, session);

        return CreatedAtAction(nameof(Get), new { id = session.Id }, GameMapper.ToResponse(session));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GameStateResponse> Get(Guid id) => Show(Owned(id));

    [HttpGet("{id:guid}/preview/{entryId:int}")]
    public ActionResult<StatsPreviewResponse> Preview(Guid id, int entryId, [FromQuery] EquipmentSlot? hand)
    {
        var session = Owned(id);

        if (session is null)
            return NotFound();

        lock (session.Game)
        {
            var preview = session.Game.Player.PreviewEquip(entryId, hand, VaultContent.Instance);

            return new StatsPreviewResponse(
                preview.Result,
                preview.MaxHealth,
                preview.MaxMana,
                preview.Attack,
                preview.Defense,
                preview.Evasion,
                preview.Critical,
                preview.Block,
                preview.Resistance);
        }
    }

    [HttpPost("{id:guid}/move")]
    public ActionResult<TurnResponse> Move(Guid id, MoveRequest request) =>
        Play(id, game => game.MovePlayer(request.Direction));

    [HttpPost("{id:guid}/face")]
    public ActionResult<TurnResponse> Face(Guid id, MoveRequest request) =>
        Play(id, game => game.Face(request.Direction));

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

    [HttpPost("{id:guid}/buy")]
    public ActionResult<TurnResponse> Buy(Guid id, OfferRequest request) =>
        Play(id, game => game.Buy(request.OfferId));

    [HttpPost("{id:guid}/travel")]
    public ActionResult<TurnResponse> Travel(Guid id, TravelRequest request) =>
        Play(id, game => game.Travel(request.Floor));

    [HttpPost("{id:guid}/totem")]
    public ActionResult<TurnResponse> AwakenTotem(Guid id) =>
        Play(id, game => game.AwakenTotem());

    [HttpPost("{id:guid}/buy-upgrade")]
    public ActionResult<TurnResponse> BuyBackpackUpgrade(Guid id) =>
        Play(id, game => game.BuyBackpackUpgrade());

    [HttpPost("{id:guid}/sell")]
    public ActionResult<TurnResponse> Sell(Guid id, EntryRequest request) =>
        Play(id, game => game.Sell(request.EntryId));

    [HttpPost("{id:guid}/sell-junk")]
    public ActionResult<TurnResponse> SellJunk(Guid id) =>
        Play(id, game => game.SellJunk());

    [HttpPost("{id:guid}/spell")]
    public ActionResult<TurnResponse> SelectSpell(Guid id, SpellRequest request) =>
        Play(id, game => game.SelectSpell(request.Index));

    [HttpPost("{id:guid}/learn")]
    public ActionResult<TurnResponse> LearnSpell(Guid id, LearnRequest request) =>
        Play(id, game => game.LearnSpell(request.EntryId, request.Key));

    private ActionResult<GameStateResponse> Show(GameSession? session)
    {
        if (session is null)
            return NotFound();

        lock (session.Game)
        {
            return GameMapper.ToResponse(session);
        }
    }

    private ActionResult<TurnResponse> Play(Guid id, Func<Game, TurnResult> action)
    {
        var session = Owned(id);

        if (session is null)
            return NotFound();

        lock (session.Game)
        {
            var result = action(session.Game);
            Save(session);
            return GameMapper.ToResponse(session, result, Trophies(session, result));
        }
    }

    private GameSession? Owned(Guid id) =>
        User.AccountId() is { } accountId && _accounts.Owns(accountId, id) ? _store.Find(id) : null;

    private void GrantDragonShard(Guid accountId, GameSession session)
    {
        try
        {
            var tier = _trophies.DragonShardTier(accountId);
            if (tier == 0)
                return;

            var element = Random.Shared.GetItems(Enum.GetValues<Element>(), 1)[0];

            lock (session.Game)
            {
                var player = session.Game.Player;
                var added = false;

                foreach (var shard in ItemCatalog.DragonShardSet(player.Class?.Id, tier, element))
                {
                    var rolled = ItemInstance.CanHaveElement(shard) && shard.FixedElement is null ? element : (Element?)null;
                    added |= player.Inventory.TryAddBound(new ItemInstance(shard, rolled));
                }

                if (added)
                    Save(session);
            }
        }
        catch (Exception error)
        {
            _logger.LogError(error, "Could not hand the Scheggia del Drago to game {GameId}.", session.Id);
        }
    }

    private IReadOnlyList<TrophyDefinition> Trophies(GameSession session, TurnResult result)
    {
        try
        {
            return _trophies.Record(session, result);
        }
        catch (Exception error)
        {
            _logger.LogError(error, "Could not record trophies for game {GameId}.", session.Id);
            return [];
        }
    }

    private void Save(GameSession session)
    {
        try
        {
            _store.Save(session);
        }
        catch (Exception error)
        {
            _logger.LogError(error, "Could not save game {GameId}.", session.Id);
        }
    }
}
