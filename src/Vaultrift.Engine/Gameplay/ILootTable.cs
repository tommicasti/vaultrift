using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public interface ILootTable
{
    ItemInstance? RollDrop(Enemy enemy, LootContext context);

    ItemInstance? RollChest(LootContext context);
}
