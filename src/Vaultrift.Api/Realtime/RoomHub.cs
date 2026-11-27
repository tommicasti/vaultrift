using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Hosting;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Realtime;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Realtime;

[Authorize]
public sealed class RoomHub(RoomRegistry rooms, VaultService vault, TimeProvider time) : Hub
{
    public const string HostLeftMessage = "The host left: the world closed.";
    public const int Protocol = 3;

    public async Task<LiveWelcome> Create(Guid heroId, Guid worldId, bool shared)
    {
        var accountId = AccountId();
        var hero = vault.FindHero(accountId, heroId) ?? throw new HubException("That hero no longer exists.");
        var world = vault.FindWorld(accountId, worldId) ?? throw new HubException("That world no longer exists.");

        switch (LiveArena.Refusal(world, shared))
        {
            case JoinRefusal.Hardcore:
                throw new HubException("Hardcore worlds are played alone.");
            case JoinRefusal.NotHard:
                throw new HubException("Co-op worlds are always on Hard.");
        }

        await NotifyClosed(rooms.Leave(Context.ConnectionId));
        Claim(hero);

        var (opening, room) = rooms.Create(Context.ConnectionId, world, shared, time.GetUtcNow());

        if (room is null)
        {
            rooms.Release(Context.ConnectionId);
            throw new HubException(opening == RoomOpening.WorldBusy
                ? "That world is already open."
                : "Too many worlds are open right now. Try again in a few minutes.");
        }

        return room.Enter(Context.ConnectionId, hero, time.GetUtcNow())!;
    }

    public async Task<LiveWelcome> Join(string code, Guid heroId)
    {
        var room = rooms.Find(code) is { IsShared: true } found ? found : throw new HubException("No open world has that code.");
        var hero = vault.FindHero(AccountId(), heroId) ?? throw new HubException("That hero no longer exists.");

        await NotifyClosed(rooms.Leave(Context.ConnectionId));
        Claim(hero);
        rooms.Track(Context.ConnectionId, room);

        if (room.Enter(Context.ConnectionId, hero, time.GetUtcNow()) is { } welcome)
            return welcome;

        await NotifyClosed(rooms.Leave(Context.ConnectionId));
        rooms.Release(Context.ConnectionId);
        throw new HubException($"That world is full ({LiveRules.MaxHeroes} heroes).");
    }

    public LiveWelcome Resume(string token) =>
        rooms.Resume(token, AccountId(), Context.ConnectionId, time.GetUtcNow())
        ?? throw new HubException("Your place in that world is gone.");

    public async Task Leave()
    {
        await NotifyClosed(rooms.Leave(Context.ConnectionId));
        rooms.Release(Context.ConnectionId);
    }

    public long Ping(long clientTime) => clientTime;

    public void Input(double moveX, double moveY, bool attack, double aimX, double aimY, bool sprint) =>
        rooms.RoomOf(Context.ConnectionId)?.SetInput(Context.ConnectionId, HeroInput.Of(moveX, moveY, attack, aimX, aimY, sprint), time.GetUtcNow());

    public LiveActionResponse Equip(int entryId, EquipmentSlot? hand) =>
        Act(act => LiveActions.Equip(act.Player, entryId, hand));

    public LiveActionResponse Unequip(EquipmentSlot slot) =>
        Act(act => LiveActions.Unequip(act.Player, slot));

    public LiveActionResponse Use(int entryId) =>
        Act(act => act.Player.Inventory.Find(entryId)?.Item.Definition is ScrollDefinition
            ? act.Floor.ReadScroll(act.Hero.Id, entryId)
            : LiveActions.Use(act.Player, entryId));

    public LiveActionResponse UseQuickSlot(int slot) =>
        Act(act => act.Player.QuickSlotEntry(slot) is { Item.Definition: ScrollDefinition } scroll
            ? act.Floor.ReadScroll(act.Hero.Id, scroll.Id)
            : LiveActions.UseQuickSlot(act.Player, slot));

    public LiveActionResponse AssignQuickSlot(int entryId, int slot) =>
        Act(act => act.Game.AssignQuickSlot(entryId, slot));

    public LiveActionResponse SpendPoint(StatPoint stat) =>
        Act(act => act.Game.SpendPoint(stat));

    public LiveActionResponse Discard(int entryId) =>
        Act(act => act.Game.Discard(entryId));

    public LiveActionResponse DiscardJunk() =>
        Act(act => act.Game.DiscardJunk());

    public LiveActionResponse Mark(int entryId, ItemMark mark) =>
        Act(act => act.Game.Mark(entryId, mark));

    public LiveActionResponse Learn(int entryId, int key) =>
        Act(act => act.Game.LearnSpell(entryId, key));

    public LiveActionResponse Ability() =>
        Act(act => act.Floor.UseAbility(act.Hero.Id));

    public LiveActionResponse SelectSpell(int index) =>
        Act(act => act.Game.SelectSpell(index));

    public LiveActionResponse Buy(int offerId) =>
        Act(act => act.Floor.Buy(act.Hero.Id, offerId));

    public LiveActionResponse BuyUpgrade() =>
        Act(act => act.Floor.BuyBackpackUpgrade(act.Hero.Id));

    public LiveActionResponse Sell(int entryId) =>
        Act(act => act.Floor.Sell(act.Hero.Id, entryId));

    public LiveActionResponse SellJunk() =>
        Act(act => act.Floor.SellJunk(act.Hero.Id));

    public void Travel(int floor)
    {
        if (CurrentRoom().Travel(Context.ConnectionId, floor) is { } refusal)
            throw new HubException(refusal);
    }

    public StatsPreviewResponse Preview(int entryId, EquipmentSlot? hand) =>
        CurrentRoom().Preview(Context.ConnectionId, entryId, hand) ?? throw new HubException("You are not in a world.");

    public StatsPreviewResponse PreviewOffer(int offerId, EquipmentSlot? hand) =>
        CurrentRoom().PreviewOffer(Context.ConnectionId, offerId, hand) ?? throw new HubException("You are not in a world.");

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!rooms.Park(Context.ConnectionId, time.GetUtcNow()))
        {
            await NotifyClosed(rooms.Leave(Context.ConnectionId));
            rooms.Release(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private LiveActionResponse Act(Func<LiveAct, TurnResult> action) =>
        CurrentRoom().Act(Context.ConnectionId, action, time.GetUtcNow()) ?? throw new HubException("You are not in a world.");

    private Room CurrentRoom() => rooms.RoomOf(Context.ConnectionId) ?? throw new HubException("You are not in a world.");

    private Guid AccountId() =>
        Context.User?.AccountId() ?? throw new HubException("Sign in to play.");

    private void Claim(HeroSession hero)
    {
        if (!rooms.TryClaim(Context.ConnectionId, hero.Id))
            throw new HubException("That hero is already in a world.");
    }

    private Task NotifyClosed(IReadOnlyList<string> connections) =>
        connections.Count == 0 ? Task.CompletedTask : Clients.Clients(connections).SendAsync("Closed", HostLeftMessage);
}
