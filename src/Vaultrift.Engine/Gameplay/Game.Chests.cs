using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    private readonly Dictionary<Position, Chest> _chests = [];

    public IReadOnlyCollection<Position> Chests => _chests.Keys;

    private bool TryOpenChest(List<GameEvent> events)
    {
        var front = Player.Position.Move(Player.Facing);

        if (!_chests.Remove(front, out var chest))
            return false;

        if (chest.Mimic is { } mimic)
        {
            mimic.IsAwake = true;
            _enemies.Add(mimic);
            events.Add(new MimicRevealed(mimic.Id, front));
            return true;
        }

        var item = _loot?.RollChest(new LootContext(Player.Class?.Id, Player.EffectAmount(EffectKind.Luck)));

        if (item is not null)
            _floorItems[front] = item;

        events.Add(new ChestOpened(front, item));
        return true;
    }

    private bool IsChest(Position position) => _chests.ContainsKey(position);

    private void LoadChests(Floor floor)
    {
        _chests.Clear();

        foreach (var chest in floor.Chests ?? [])
            _chests.Add(chest.Position, chest);
    }
}
