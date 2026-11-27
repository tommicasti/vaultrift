using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Realtime;

public static class DragonReward
{
    public static int TierFor(Difficulty difficulty) => Math.Clamp((int)difficulty + 1, 1, ItemCatalog.DragonShardTiers);

    public static IReadOnlyList<ItemInstance> For(string? classId, Difficulty difficulty, IRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var elements = Enum.GetValues<Element>();
        var element = elements[Math.Min(elements.Length - 1, (int)(random.NextDouble() * elements.Length))];

        return ItemCatalog.DragonShardSet(classId, TierFor(difficulty), element)
            .Select(shard => new ItemInstance(shard, ItemInstance.CanHaveElement(shard) && shard.FixedElement is null ? element : null))
            .ToList();
    }
}
