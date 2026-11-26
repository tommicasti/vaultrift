using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vaultrift.Api.Contracts;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Realtime;

[Authorize]
public sealed class RoomHub(RoomRegistry rooms, TimeProvider time) : Hub
{
    public const string HostLeftMessage = "The host left: the world closed.";

    public LiveWelcome Create(string classId, string? heroName)
    {
        var session = NewHero(classId, heroName);
        var room = rooms.Create(Context.ConnectionId, session, new SeededRandom((ulong)Random.Shared.NextInt64()), time.GetUtcNow())
            ?? throw new HubException("Too many worlds are open right now. Try again in a few minutes.");

        return room.Enter(Context.ConnectionId, session, time.GetUtcNow())!;
    }

    public async Task<LiveWelcome> Join(string code, string classId, string? heroName)
    {
        var room = rooms.Find(code) ?? throw new HubException("No open world has that code.");
        var session = NewHero(classId, heroName);

        await NotifyClosed(rooms.Leave(Context.ConnectionId));
        rooms.Track(Context.ConnectionId, room);

        return room.Enter(Context.ConnectionId, session, time.GetUtcNow())
            ?? throw new HubException($"That world is full ({LiveRules.MaxHeroes} heroes).");
    }

    public void Input(double moveX, double moveY, bool attack, double aimX, double aimY) =>
        rooms.RoomOf(Context.ConnectionId)?.SetInput(Context.ConnectionId, HeroInput.Of(moveX, moveY, attack, aimX, aimY), time.GetUtcNow());

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

    public StatsPreviewResponse Preview(int entryId, EquipmentSlot? hand) =>
        CurrentRoom().Preview(Context.ConnectionId, entryId, hand) ?? throw new HubException("You are not in a world.");

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await NotifyClosed(rooms.Leave(Context.ConnectionId));
        await base.OnDisconnectedAsync(exception);
    }

    private LiveActionResponse Act(Func<LiveAct, TurnResult> action) =>
        CurrentRoom().Act(Context.ConnectionId, action, time.GetUtcNow()) ?? throw new HubException("You are not in a world.");

    private Room CurrentRoom() => rooms.RoomOf(Context.ConnectionId) ?? throw new HubException("You are not in a world.");

    private static GameSession NewHero(string classId, string? heroName)
    {
        var playerClass = ClassCatalog.Find(classId) ?? throw new HubException("Unknown class.");
        var name = string.IsNullOrWhiteSpace(heroName) ? playerClass.Class.Name : heroName.Trim();

        if (name.Length > Player.MaxNameLength)
            name = name[..Player.MaxNameLength];

        return GameSession.Start(Guid.NewGuid(), "LIVE", playerClass, name);
    }

    private Task NotifyClosed(IReadOnlyList<string> connections) =>
        connections.Count == 0 ? Task.CompletedTask : Clients.Clients(connections).SendAsync("Closed", HostLeftMessage);
}
