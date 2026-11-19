using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Content;

public static class SpellCatalog
{
    public static SpellDefinition FireBolt { get; } = new("fire-bolt", "Fire Bolt", Element.Fire, SpellShape.Bolt, power: 4, manaCost: 3);

    public static SpellDefinition IceShard { get; } = new("ice-shard", "Ice Shard", Element.Ice, SpellShape.Bolt, power: 4, manaCost: 3);

    public static SpellDefinition Spark { get; } = new("spark", "Spark", Element.Lightning, SpellShape.Bolt, power: 4, manaCost: 3);

    public static SpellDefinition Gust { get; } = new("gust", "Gust", Element.Wind, SpellShape.Bolt, power: 4, manaCost: 3, alwaysTriggers: true);

    public static IReadOnlyList<SpellDefinition> All { get; } =
    [
        FireBolt,
        IceShard,
        Spark,
        Gust,
        new("fireball", "Fireball", Element.Fire, SpellShape.Burst, power: 5, manaCost: 6, size: 1),
        new("frost-nova", "Frost Nova", Element.Ice, SpellShape.Nova, power: 3, manaCost: 6, size: 1, alwaysTriggers: true),
        new("chain-lightning", "Chain Lightning", Element.Lightning, SpellShape.Chain, power: 5, manaCost: 6, size: 3),
        new("squall", "Squall", Element.Wind, SpellShape.Line, power: 5, manaCost: 6, size: 2),
        new("fire-rain", "Fire Rain", Element.Fire, SpellShape.Burst, power: 6, manaCost: 10, size: 2, alwaysTriggers: true, manaPercent: 25),
        new("blizzard", "Blizzard", Element.Ice, SpellShape.Nova, power: 5, manaCost: 10, size: 2, manaPercent: 25),
        new("tempest", "Tempest", Element.Lightning, SpellShape.Storm, power: 6, manaCost: 10, size: 5, manaPercent: 25),
        new("tornado", "Tornado", Element.Wind, SpellShape.Burst, power: 6, manaCost: 10, size: 1, manaPercent: 25)
    ];

    public static SpellDefinition Get(string id) =>
        All.FirstOrDefault(spell => spell.Id == id)
        ?? throw new KeyNotFoundException($"Unknown spell '{id}'.");
}
