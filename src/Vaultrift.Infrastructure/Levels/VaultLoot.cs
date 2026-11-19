using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Levels;

public sealed class VaultLoot : ILootTable
{
    public const double PotionShare = 0.35;
    public const double ClassModShare = 0.10;
    public const double BossClassModShare = 0.25;
    public const double OwnClassModShare = 0.70;
    public const double ChestPotionShare = 0.20;
    public const double ScrollShare = 0.25;
    public const double EpicScrollShare = 0.25;

    private static readonly string[] Potions =
        ["potion-healing", "potion-greater-healing", "potion-mana", "potion-greater-mana", "potion-strength", "potion-protection"];

    private static readonly Rarity[] RarestFirst = [Rarity.Legendary, Rarity.Epic, Rarity.Rare];

    private readonly IRandom _random;

    public VaultLoot(IRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public static double DropChance(Enemy enemy, int luck = 0)
    {
        ArgumentNullException.ThrowIfNull(enemy);

        if (KeepsTreasure(enemy))
            return 1.0;

        var chance = enemy.Rarity switch
        {
            Rarity.Common => 0.12,
            Rarity.Rare => 0.25,
            Rarity.Epic => 0.45,
            Rarity.Legendary => 0.75,
            _ => throw new ArgumentOutOfRangeException(nameof(enemy), enemy.Rarity, "Unknown rarity.")
        };

        return Math.Min(1.0, chance * (1 + luck / 100.0));
    }

    public static double DropRarityChance(Enemy enemy, Rarity drop)
    {
        ArgumentNullException.ThrowIfNull(enemy);

        var (legendary, epic, rare) = enemy.Kind.IsStationary ? (0.06, 0.34, 0.60)
            : enemy.Kind.IsBoss ? enemy.Rarity switch
            {
                Rarity.Common => (0.05, 0.35, 0.60),
                Rarity.Rare => (0.10, 0.45, 0.45),
                _ => (0.20, 0.50, 0.30)
            }
            : enemy.Rarity switch
            {
                Rarity.Common => (0.003, 0.04, 0.20),
                Rarity.Rare => (0.01, 0.10, 0.35),
                Rarity.Epic => (0.03, 0.25, 0.42),
                Rarity.Legendary => (0.12, 0.40, 0.48),
                _ => throw new ArgumentOutOfRangeException(nameof(enemy), enemy.Rarity, "Unknown rarity.")
            };

        return Share(drop, legendary, epic, rare);
    }

    public static double ChestRarityChance(Rarity drop) => Share(drop, legendary: 0.02, epic: 0.12, rare: 0.40);

    public ItemInstance? RollChest(LootContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_random.NextDouble() < ChestPotionShare)
            return RollConsumable();

        var rarity = Roll(ChestRarityChance);

        if (RollClassMod(rarity, ClassModShare, context.ClassId) is { } classMod)
            return new ItemInstance(classMod);

        return RollEquipment(rarity);
    }

    private static double Share(Rarity drop, double legendary, double epic, double rare) => drop switch
    {
        Rarity.Legendary => legendary,
        Rarity.Epic => epic,
        Rarity.Rare => rare,
        Rarity.Common => 1.0 - legendary - epic - rare,
        _ => throw new ArgumentOutOfRangeException(nameof(drop), drop, "Unknown rarity.")
    };

    private static bool KeepsTreasure(Enemy enemy) => enemy.Kind.IsBoss || enemy.Kind.IsStationary;

    public ItemInstance? RollDrop(Enemy enemy, LootContext context)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(context);

        if (_random.NextDouble() >= DropChance(enemy, context.Luck))
            return null;

        if (!KeepsTreasure(enemy) && _random.NextDouble() < PotionShare)
            return RollConsumable();

        var rarity = Roll(drop => DropRarityChance(enemy, drop));
        var share = KeepsTreasure(enemy) ? BossClassModShare : ClassModShare;

        if (RollClassMod(rarity, share, context.ClassId) is { } classMod)
            return new ItemInstance(classMod);

        return RollEquipment(rarity);
    }

    private ItemInstance RollConsumable()
    {
        if (_random.NextDouble() >= ScrollShare)
            return new ItemInstance(ItemCatalog.Get(_random.Pick(Potions)));

        var rarity = _random.NextDouble() < EpicScrollShare ? Rarity.Epic : Rarity.Rare;
        return new ItemInstance(_random.Pick(ItemCatalog.Scrolls.Where(scroll => scroll.Rarity == rarity).ToList()));
    }

    private ItemInstance RollEquipment(Rarity rarity)
    {
        var candidates = ItemCatalog.Equipment
            .Where(item => item.Rarity == rarity)
            .ToList();

        return ItemRoller.Roll(_random.Pick(candidates), _random);
    }

    private GearDefinition? RollClassMod(Rarity rarity, double share, string? classId)
    {
        var mods = ItemCatalog.ClassMods.Where(mod => mod.Rarity == rarity).ToList();

        if (mods.Count == 0)
            return null;

        if (_random.NextDouble() >= share)
            return null;

        var own = mods.Where(mod => mod.ClassId == classId).ToList();
        var others = mods.Where(mod => mod.ClassId != classId).ToList();

        if (own.Count > 0 && (others.Count == 0 || _random.NextDouble() < OwnClassModShare))
            return _random.Pick(own);

        return _random.Pick(others);
    }

    private Rarity Roll(Func<Rarity, double> chance)
    {
        var roll = _random.NextDouble();
        var threshold = 0.0;

        foreach (var rarity in RarestFirst)
        {
            threshold += chance(rarity);

            if (roll < threshold)
                return rarity;
        }

        return Rarity.Common;
    }
}
