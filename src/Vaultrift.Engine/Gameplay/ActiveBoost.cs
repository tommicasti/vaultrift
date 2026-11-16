using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public sealed class ActiveBoost
{
    internal ActiveBoost(PotionDefinition source, StatModifier modifier, int remainingTurns)
    {
        Source = source;
        Modifier = modifier;
        RemainingTurns = remainingTurns;
    }

    public PotionDefinition Source { get; }

    public StatModifier Modifier { get; }

    public int RemainingTurns { get; internal set; }
}