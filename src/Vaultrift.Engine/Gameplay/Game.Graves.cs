using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public Difficulty Difficulty { get; init; }

    public Grave? Grave { get; private set; }

    public bool IsLost { get; private set; }

    public bool LeavesGraves => Difficulty != Difficulty.Normal;

    private bool HandleDeath(List<GameEvent> events)
    {
        if (Difficulty == Difficulty.Hardcore)
        {
            Deaths++;
            IsLost = true;
            events.Add(new PlayerDefeated());
            events.Add(new HeroLost());
            return true;
        }

        return false;
    }

    private void DigGrave(List<GameEvent> events)
    {
        if (Grave is { } lost)
            events.Add(new GraveLost(lost.Position, lost.Count));

        var items = Player.StripEquipment();
        Grave = items.Count == 0 ? null : new Grave(Player.Position, items);

        if (Grave is { } grave)
            events.Add(new GraveDug(grave.Position, grave.Count));
    }

    private void TakeSpareWeapon(List<GameEvent> events)
    {
        if (LeavesGraves && Player.TakeSpareWeapon())
            events.Add(new SpareWeaponTaken(Player.Equipment[EquipmentSlot.MainHand].Item));
    }

    private void RecoverGraveAt(Position position, List<GameEvent> events)
    {
        if (Grave is not { } grave || grave.Position != position)
            return;

        var left = Player.Reclaim(grave.Items);
        var recovered = grave.Count - left.Count;
        Grave = left.Count == 0 ? null : grave with { Items = left };

        if (recovered > 0)
            events.Add(new GraveRecovered(recovered, left.Count));
    }

    private void AbandonGrave(List<GameEvent> events)
    {
        if (Grave is not { } grave)
            return;

        Grave = null;
        events.Add(new GraveLost(grave.Position, grave.Count));
    }

    private TurnResult Defeated() => new(TurnPassed: false, [new HeroLost()]);
}
