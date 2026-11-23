using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Combat;

public static class ElementRules
{
    public const double ResistanceMultiplier = 0.5;
    public const int BurnTurns = 3;
    public const int FreezeTurns = 1;
    public const int FreezeImmunityTurns = 3;

    public static double WeaknessMultiplier(Rarity rarity) =>
        rarity >= Rarity.Legendary ? 2.0 : 1.5;

    public static int BurnDamage(Rarity rarity) =>
        rarity >= Rarity.Legendary ? 3 : 2;

    public static double EffectChance(Element element, Rarity rarity) => (element, rarity) switch
    {
        (Element.Fire, Rarity.Common) => 0.10,
        (Element.Fire, Rarity.Rare) => 0.15,
        (Element.Fire, Rarity.Epic) => 0.20,
        (Element.Fire, Rarity.Legendary or Rarity.Exotic or Rarity.Platinum) => 0.40,
        (Element.Ice, Rarity.Common) => 0.08,
        (Element.Ice, Rarity.Rare) => 0.12,
        (Element.Ice, Rarity.Epic) => 0.18,
        (Element.Ice, Rarity.Legendary or Rarity.Exotic or Rarity.Platinum) => 0.30,
        (Element.Lightning or Element.Wind, Rarity.Common) => 0.25,
        (Element.Lightning or Element.Wind, Rarity.Rare) => 0.40,
        (Element.Lightning or Element.Wind, Rarity.Epic) => 0.60,
        (Element.Lightning or Element.Wind, Rarity.Legendary or Rarity.Exotic or Rarity.Platinum) => 1.0,
        _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown element or rarity.")
    };

    public static Effectiveness EffectivenessOf(Element element, Element? weakness, Element? resistance)
    {
        if (element == weakness)
            return Effectiveness.Effective;

        return element == resistance ? Effectiveness.Resisted : Effectiveness.Neutral;
    }

    public static AttackResult Apply(AttackResult result, Element? element, Element? weakness, Element? resistance, Rarity sourceRarity)
    {
        if (element is not { } attackElement)
            return result;

        if (result.Missed)
            return result with { Element = attackElement };

        var effectiveness = EffectivenessOf(attackElement, weakness, resistance);
        var multiplier = effectiveness switch
        {
            Effectiveness.Effective => WeaknessMultiplier(sourceRarity),
            Effectiveness.Resisted => ResistanceMultiplier,
            _ => 1.0
        };

        var damage = Math.Max(
            DamageCalculator.MinimumDamage,
            (int)Math.Round(result.Damage * multiplier, MidpointRounding.AwayFromZero));

        return result with { Damage = damage, Element = attackElement, Effectiveness = effectiveness };
    }
}
