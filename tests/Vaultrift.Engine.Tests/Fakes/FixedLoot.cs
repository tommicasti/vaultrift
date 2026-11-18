using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Tests.Fakes;

public sealed class FixedLoot : ILootTable
{
    private readonly ItemInstance? _drop;

    public FixedLoot(ItemInstance? drop)
    {
        _drop = drop;
    }

    public ItemInstance? RollDrop(Enemy enemy, LootContext context) => _drop;

    public ItemInstance? RollChest(LootContext context) => _drop;
}
