using Vaultrift.Engine.Items;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Tests.Content;

public class ItemCatalogTests
{
    private static readonly EquipmentKind[] GearKinds =
        [EquipmentKind.Shield, EquipmentKind.Head, EquipmentKind.Body, EquipmentKind.Feet, EquipmentKind.Ring, EquipmentKind.Cloak];

    private static readonly Rarity[] NonLegendary = [Rarity.Common, Rarity.Rare, Rarity.Epic];

    private static IEnumerable<EquipmentDefinition> Equipment => ItemCatalog.All.OfType<EquipmentDefinition>();

    private static IEnumerable<WeaponDefinition> Weapons => ItemCatalog.All.OfType<WeaponDefinition>();

    private static IEnumerable<GearDefinition> Gear => ItemCatalog.All.OfType<GearDefinition>();

    private static IEnumerable<PotionDefinition> Potions => ItemCatalog.All.OfType<PotionDefinition>();

    [Fact]
    public void All_IdsAreUnique()
    {
        var duplicates = ItemCatalog.All
            .GroupBy(item => item.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Equipment_StaysWithinTheModifierBudgetOfItsRarity()
    {
        Assert.All(Equipment, item => Assert.InRange(item.Modifiers.Count, 0, ModifierBudget(item.Rarity)));
    }

    [Fact]
    public void Equipment_AlwaysGivesSomething()
    {
        Assert.All(Equipment, item => Assert.True(item.Modifiers.Count + item.Effects.Count > 0, item.Id));
    }

    [Fact]
    public void Equipment_AllModifiersAndEffectsArePositive()
    {
        Assert.All(Equipment.SelectMany(item => item.Modifiers), modifier => Assert.True(modifier.Amount > 0));
        Assert.All(Equipment.SelectMany(item => item.Effects), effect => Assert.True(effect.Amount > 0));
    }

    [Fact]
    public void Weapons_GiveAttack()
    {
        Assert.All(Weapons, weapon => Assert.Contains(weapon.Modifiers, modifier => modifier.Stat == StatType.Attack));
    }

    [Fact]
    public void Gear_IsNeverAWeapon()
    {
        Assert.All(Gear, item => Assert.NotEqual(EquipmentKind.Weapon, item.Kind));
    }

    [Theory]
    [InlineData(EquipmentKind.Shield, StatType.Block)]
    [InlineData(EquipmentKind.Shield, StatType.Defense)]
    [InlineData(EquipmentKind.Head, StatType.Defense)]
    [InlineData(EquipmentKind.Body, StatType.Defense)]
    [InlineData(EquipmentKind.Feet, StatType.Evasion)]
    [InlineData(EquipmentKind.Cloak, StatType.Evasion)]
    public void Gear_GivesThePrincipalBonusOfItsKind(EquipmentKind kind, StatType principal)
    {
        Assert.All(
            Gear.Where(item => item.Kind == kind),
            item => Assert.Contains(item.Modifiers, modifier => modifier.Stat == principal));
    }

    [Fact]
    public void EveryWeaponType_HasTwoCommonsTwoRaresFourEpicsAndFiveLegendaries()
    {
        foreach (var type in Enum.GetValues<WeaponType>())
        {
            Assert.Equal(2, Weapons.Count(weapon => weapon.WeaponType == type && weapon.Rarity == Rarity.Common));
            Assert.Equal(2, Weapons.Count(weapon => weapon.WeaponType == type && weapon.Rarity == Rarity.Rare));
            Assert.Equal(4, Weapons.Count(weapon => weapon.WeaponType == type && weapon.Rarity == Rarity.Epic));
            Assert.Equal(5, Weapons.Count(weapon => weapon.WeaponType == type && weapon.Rarity == Rarity.Legendary));
        }
    }

    [Theory]
    [InlineData(WeaponType.Axe)]
    [InlineData(WeaponType.Spear)]
    public void TwoHandedMeleeWeapons_HitHarderThanSwordsOfTheSameRarity(WeaponType type)
    {
        foreach (var rarity in NonLegendary)
            Assert.True(BestAttack(type, rarity) > BestAttack(WeaponType.Sword, rarity), $"{type} {rarity}");
    }

    [Fact]
    public void EveryGearKind_ExistsInEveryRarity()
    {
        foreach (var kind in GearKinds)
            foreach (var rarity in Enum.GetValues<Rarity>())
                Assert.Contains(Gear, item => item.Kind == kind && item.Rarity == rarity);
    }

    [Fact]
    public void EveryGearKind_HasThreeLegendaries()
    {
        foreach (var kind in GearKinds)
            Assert.Equal(3, Gear.Count(item => item.Kind == kind && item.Rarity == Rarity.Legendary));
    }

    [Fact]
    public void Legendaries_HaveADescription()
    {
        Assert.All(
            ItemCatalog.All.Where(item => item.Rarity == Rarity.Legendary),
            item => Assert.False(string.IsNullOrWhiteSpace(item.Description)));
    }

    [Fact]
    public void FixedElements_BelongOnlyToLegendaryWeaponsAndCloaks()
    {
        Assert.All(
            Equipment.Where(item => item.FixedElement is not null && item is not WeaponDefinition { WeaponType: WeaponType.Staff }),
            item =>
            {
                Assert.Equal(Rarity.Legendary, item.Rarity);
                Assert.True(ItemInstance.CanHaveElement(item));
            });
    }

    [Fact]
    public void EveryStaff_CarriesASpell_OfItsOwnElement()
    {
        var staffs = Equipment.OfType<WeaponDefinition>().Where(item => item.WeaponType == WeaponType.Staff).ToList();

        Assert.NotEmpty(staffs);
        Assert.All(staffs, staff =>
        {
            Assert.NotNull(staff.Spell);
            Assert.Equal(SpellShape.Bolt, staff.Spell!.Shape);
            Assert.Equal(staff.Spell.Element, staff.FixedElement);
        });
    }

    [Fact]
    public void Scrolls_TeachEveryNonBoltSpell_RareOrEpic()
    {
        var taught = ItemCatalog.Scrolls.Select(scroll => scroll.Spell.Id).ToList();

        Assert.Equal(SpellCatalog.All.Where(spell => spell.Shape != SpellShape.Bolt).Select(spell => spell.Id).Order().ToList(), taught.Order().ToList());
        Assert.All(ItemCatalog.Scrolls, scroll => Assert.Contains(scroll.Rarity, new[] { Rarity.Rare, Rarity.Epic }));
    }

    [Fact]
    public void EpicSpells_CostAQuarterOfMaximumMana_WhenThatIsMore()
    {
        var epic = ItemCatalog.Scrolls.Where(scroll => scroll.Rarity == Rarity.Epic).Select(scroll => scroll.Spell).ToList();
        var others = SpellCatalog.All.Except(epic).ToList();

        Assert.All(epic, spell => Assert.Equal(25, spell.ManaPercent));
        Assert.All(others, spell => Assert.Equal(0, spell.ManaPercent));
    }

    [Fact]
    public void StrongerSpells_CostMoreMana()
    {
        var bolts = SpellCatalog.All.Where(spell => spell.Shape == SpellShape.Bolt).ToList();
        var rare = ItemCatalog.Scrolls.Where(scroll => scroll.Rarity == Rarity.Rare).Select(scroll => scroll.Spell).ToList();
        var epic = ItemCatalog.Scrolls.Where(scroll => scroll.Rarity == Rarity.Epic).Select(scroll => scroll.Spell).ToList();

        Assert.True(bolts.Max(spell => spell.ManaCost) < rare.Min(spell => spell.ManaCost));
        Assert.True(rare.Max(spell => spell.ManaCost) < epic.Min(spell => spell.ManaCost));
    }

    [Fact]
    public void EveryElement_HasALegendaryWeapon()
    {
        foreach (var element in Enum.GetValues<Element>())
            Assert.Contains(Weapons, weapon => weapon.Rarity == Rarity.Legendary && weapon.FixedElement == element);
    }

    [Theory]
    [InlineData(EffectKind.FireBoost)]
    [InlineData(EffectKind.IceBoost)]
    [InlineData(EffectKind.LightningBoost)]
    [InlineData(EffectKind.WindBoost)]
    public void EveryElement_HasARingThatBoostsIt(EffectKind boost)
    {
        Assert.Contains(
            Gear,
            item => item.Kind == EquipmentKind.Ring && item.Effects.Any(effect => effect.Kind == boost));
    }

    [Fact]
    public void LegendaryCloaks_AlwaysHaveAnElement()
    {
        Assert.All(
            Gear.Where(item => item.Kind == EquipmentKind.Cloak && item.Rarity == Rarity.Legendary),
            cloak => Assert.NotNull(cloak.FixedElement));
    }

    [Fact]
    public void EveryClass_HasARareAnEpicAndALegendaryClassMod()
    {
        foreach (var definition in ClassCatalog.All)
        {
            var mods = ItemCatalog.ClassMods.Where(mod => mod.ClassId == definition.Class.Id).ToList();

            Assert.Equal(new List<Rarity> { Rarity.Rare, Rarity.Epic, Rarity.Legendary }, mods.Select(mod => mod.Rarity).Order().ToList());
            Assert.All(mods, mod => Assert.NotEmpty(mod.Effects));
            Assert.All(mods, mod => Assert.False(string.IsNullOrWhiteSpace(mod.Description)));
        }
    }

    [Fact]
    public void ClassMods_BelongToAnExistingClass_AndAreNotGenericEquipment()
    {
        Assert.All(ItemCatalog.ClassMods, mod => Assert.NotNull(ClassCatalog.Find(mod.ClassId!)));
        Assert.DoesNotContain(ItemCatalog.Equipment, item => item.Kind == EquipmentKind.ClassMod);
    }

    [Fact]
    public void Potions_HaveExactlyOneEffect()
    {
        Assert.All(Potions, potion =>
        {
            var effects = new[] { potion.Healing > 0, potion.ManaRestore > 0, potion.Boost is not null };

            Assert.Equal(1, effects.Count(effect => effect));
        });
    }

    [Fact]
    public void BoostPotions_LastAtLeastOneTurn()
    {
        Assert.All(Potions.Where(potion => potion.Boost is not null), potion => Assert.True(potion.BoostTurns > 0));
    }

    [Fact]
    public void Get_KnownId_ReturnsTheItem()
    {
        var item = ItemCatalog.Get("sword-vaultbreaker");

        Assert.Equal("Vaultbreaker", item.Name);
        Assert.Equal(Rarity.Legendary, item.Rarity);
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => ItemCatalog.Get("sword-imaginary"));
    }

    private static int BestAttack(WeaponType type, Rarity rarity) =>
        Weapons
            .Where(weapon => weapon.WeaponType == type && weapon.Rarity == rarity)
            .Max(weapon => weapon.Modifiers.Where(modifier => modifier.Stat == StatType.Attack).Sum(modifier => modifier.Amount));

    private static int ModifierBudget(Rarity rarity) => rarity switch
    {
        Rarity.Common or Rarity.Rare => 2,
        Rarity.Epic or Rarity.Legendary => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };
}
