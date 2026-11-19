using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    private void DropLoot(Enemy enemy, List<GameEvent> events)
    {
        if (_loot?.RollDrop(enemy, new LootContext(Player.Class?.Id, Player.EffectAmount(EffectKind.Luck))) is not { } item)
            return;

        if (DropSpot(enemy.Position) is not { } spot)
            return;

        _floorItems[spot] = item;
        events.Add(new ItemDropped(enemy.Id, item, spot));
    }

    private Position? DropSpot(Position origin)
    {
        var spots = Enum.GetValues<Direction>().Select(origin.Move).Prepend(origin);

        foreach (var spot in spots)
        {
            if (CanHoldDrop(spot))
                return spot;
        }

        return null;
    }

    private bool CanHoldDrop(Position position) =>
        Map.IsWalkable(position) &&
        !IsChest(position) &&
        !IsMerchant(position) &&
        position != Player.Position &&
        position != Stairs &&
        !_floorItems.ContainsKey(position);
}
