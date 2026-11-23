using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Tests.Combat;

public class ElementRulesTests
{
    private static readonly AttackResult Hit = new(8, IsCritical: false);

    [Fact]
    public void NoElement_LeavesTheResultUntouched()
    {
        Assert.Equal(Hit, ElementRules.Apply(Hit, null, Element.Fire, null, Rarity.Common));
    }

    [Fact]
    public void Weakness_MultipliesByOneAndAHalf()
    {
        var result = ElementRules.Apply(Hit, Element.Fire, Element.Fire, null, Rarity.Epic);

        Assert.Equal(12, result.Damage);
        Assert.Equal(Effectiveness.Effective, result.Effectiveness);
        Assert.Equal(Element.Fire, result.Element);
    }

    [Fact]
    public void Weakness_AgainstALegendary_Doubles()
    {
        Assert.Equal(16, ElementRules.Apply(Hit, Element.Fire, Element.Fire, null, Rarity.Legendary).Damage);
    }

    [Fact]
    public void Resistance_Halves()
    {
        var result = ElementRules.Apply(Hit, Element.Ice, Element.Fire, Element.Ice, Rarity.Legendary);

        Assert.Equal(4, result.Damage);
        Assert.Equal(Effectiveness.Resisted, result.Effectiveness);
    }

    [Fact]
    public void Resistance_NeverGoesBelowMinimumDamage()
    {
        var result = ElementRules.Apply(new AttackResult(1, false), Element.Ice, null, Element.Ice, Rarity.Common);

        Assert.Equal(DamageCalculator.MinimumDamage, result.Damage);
    }

    [Fact]
    public void Neutral_KeepsTheDamageButRemembersTheElement()
    {
        var result = ElementRules.Apply(Hit, Element.Wind, Element.Fire, Element.Ice, Rarity.Common);

        Assert.Equal(8, result.Damage);
        Assert.Equal(Effectiveness.Neutral, result.Effectiveness);
        Assert.Equal(Element.Wind, result.Element);
    }

    [Fact]
    public void Dodged_StaysDodged()
    {
        var result = ElementRules.Apply(AttackResult.Dodged, Element.Fire, Element.Fire, null, Rarity.Common);

        Assert.True(result.IsDodged);
        Assert.Equal(0, result.Damage);
    }

    [Theory]
    [InlineData(Element.Fire, Rarity.Common, 0.10)]
    [InlineData(Element.Fire, Rarity.Legendary, 0.40)]
    [InlineData(Element.Ice, Rarity.Rare, 0.12)]
    [InlineData(Element.Ice, Rarity.Legendary, 0.30)]
    [InlineData(Element.Lightning, Rarity.Epic, 0.60)]
    [InlineData(Element.Wind, Rarity.Legendary, 1.0)]
    public void EffectChance_GrowsWithRarity(Element element, Rarity rarity, double expected)
    {
        Assert.Equal(expected, ElementRules.EffectChance(element, rarity));
    }

    [Fact]
    public void EffectChance_IsAlwaysHigherForRarerItems_AndExoticsMatchLegendaries()
    {
        foreach (var element in Enum.GetValues<Element>())
        {
            var chances = Enum.GetValues<Rarity>().Where(rarity => rarity is not (Rarity.Exotic or Rarity.Platinum)).Select(rarity => ElementRules.EffectChance(element, rarity)).ToList();

            Assert.Equal(chances.OrderBy(chance => chance).ToList(), chances);
            Assert.Equal(chances.Count, chances.Distinct().Count());
            Assert.Equal(ElementRules.EffectChance(element, Rarity.Legendary), ElementRules.EffectChance(element, Rarity.Exotic));
        }
    }
}
