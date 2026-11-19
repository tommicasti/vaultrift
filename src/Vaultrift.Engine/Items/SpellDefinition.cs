namespace Vaultrift.Engine.Items;

public sealed record SpellDefinition
{
    public SpellDefinition(
        string id,
        string name,
        Element element,
        SpellShape shape,
        int power,
        int manaCost,
        int size = 0,
        bool alwaysTriggers = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(power);
        ArgumentOutOfRangeException.ThrowIfLessThan(manaCost, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        Id = id;
        Name = name;
        Element = element;
        Shape = shape;
        Power = power;
        ManaCost = manaCost;
        Size = size;
        AlwaysTriggers = alwaysTriggers;
    }

    public string Id { get; }

    public string Name { get; }

    public Element Element { get; }

    public SpellShape Shape { get; }

    public int Power { get; }

    public int ManaCost { get; }

    public int Size { get; }

    public bool AlwaysTriggers { get; }
}
