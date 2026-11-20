using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public sealed record StatsPreview(
    EquipResult Result,
    int MaxHealth,
    int MaxMana,
    int Attack,
    int Defense,
    int Evasion,
    int Critical,
    int Block,
    Element? Resistance)
{
    public static StatsPreview Of(EquipResult result, Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var combat = player.CombatStats;

        return new StatsPreview(
            result,
            player.Stats.MaxHealth,
            player.MaxMana,
            combat.Attack,
            combat.Defense,
            Math.Min(combat.Evasion, DamageCalculator.MaxEvasion),
            Math.Min(combat.Critical, DamageCalculator.MaxCritical),
            Math.Min(combat.Block, DamageCalculator.MaxBlock),
            player.Resistance);
    }
}
