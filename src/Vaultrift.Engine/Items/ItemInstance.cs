namespace Vaultrift.Engine.Items;

public sealed record ItemInstance
{
    public ItemInstance(ItemDefinition definition, Element? element = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var fixedElement = (definition as EquipmentDefinition)?.FixedElement;
        element ??= fixedElement;

        if (element is not null && !CanHaveElement(definition))
            throw new ArgumentException($"{definition.Name} cannot have an element.", nameof(element));

        if (fixedElement is not null && element != fixedElement)
            throw new ArgumentException($"{definition.Name} is always {fixedElement}.", nameof(element));

        Definition = definition;
        Element = element;
    }

    public ItemDefinition Definition { get; }

    public Element? Element { get; }

    public string Id => Definition.Id;

    public Rarity Rarity => Definition.Rarity;

    public string Name =>
        Element is { } element && Definition is EquipmentDefinition { FixedElement: null }
            ? $"{Prefix(element)} {Definition.Name}"
            : Definition.Name;

    public static bool CanHaveElement(ItemDefinition definition) =>
        definition is WeaponDefinition or GearDefinition { Kind: EquipmentKind.Cloak };

    public static implicit operator ItemInstance(ItemDefinition definition) => new(definition);

    private string Prefix(Element element) => Definition is WeaponDefinition
        ? element switch
        {
            Items.Element.Fire => "Blazing",
            Items.Element.Ice => "Frozen",
            Items.Element.Lightning => "Shocking",
            Items.Element.Wind => "Gusting",
            _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown element.")
        }
        : element switch
        {
            Items.Element.Fire => "Fireproof",
            Items.Element.Ice => "Frostproof",
            Items.Element.Lightning => "Insulated",
            Items.Element.Wind => "Windproof",
            _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown element.")
        };
}