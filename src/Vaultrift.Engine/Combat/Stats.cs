using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Combat;

public sealed record Stats
{
    public const int DefaultCritical = 10;

    public Stats(
        int maxHealth,
        int attack,
        int defense,
        int evasion = 0,
        int critical = DefaultCritical,
        int block = 0,
        int maxMana = 0)
    {
        if (maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be positive.");

        if (attack < 0)
            throw new ArgumentOutOfRangeException(nameof(attack), attack, "Attack cannot be negative.");

        if (defense < 0)
            throw new ArgumentOutOfRangeException(nameof(defense), defense, "Defense cannot be negative.");

        if (evasion is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(evasion), evasion, "Evasion must be between 0 and 100.");

        if (critical is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(critical), critical, "Critical must be between 0 and 100.");

        if (block is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(block), block, "Block must be between 0 and 100.");

        if (maxMana < 0)
            throw new ArgumentOutOfRangeException(nameof(maxMana), maxMana, "Max mana cannot be negative.");

        MaxHealth = maxHealth;
        Attack = attack;
        Defense = defense;
        Evasion = evasion;
        Critical = critical;
        Block = block;
        MaxMana = maxMana;
    }

    public int MaxHealth { get; }

    public int Attack { get; }

    public int Defense { get; }

    public int Evasion { get; }

    public int Critical { get; }

    public int Block { get; }

    public int MaxMana { get; }

    public Stats Apply(IEnumerable<StatModifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var attack = Attack;
        var defense = Defense;
        var evasion = Evasion;
        var critical = Critical;
        var block = Block;
        var maxMana = MaxMana;

        foreach (var modifier in modifiers)
        {
            switch (modifier.Stat)
            {
                case StatType.Attack:
                    attack += modifier.Amount;
                    break;
                case StatType.Defense:
                    defense += modifier.Amount;
                    break;
                case StatType.Evasion:
                    evasion += modifier.Amount;
                    break;
                case StatType.Critical:
                    critical += modifier.Amount;
                    break;
                case StatType.Block:
                    block += modifier.Amount;
                    break;
                case StatType.Mana:
                    maxMana += modifier.Amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifiers), modifier.Stat, "Unknown stat type.");
            }
        }

        return new Stats(
            MaxHealth,
            Math.Max(0, attack),
            Math.Max(0, defense),
            Math.Clamp(evasion, 0, 100),
            Math.Clamp(critical, 0, 100),
            Math.Clamp(block, 0, 100),
            Math.Max(0, maxMana));
    }

    public Stats WithAttack(int attack) =>
        new(MaxHealth, attack, Defense, Evasion, Critical, Block, MaxMana);

    public Stats WithDefense(int defense) =>
        new(MaxHealth, Attack, Math.Max(0, defense), Evasion, Critical, Block, MaxMana);

    public Stats WithCritical(int critical) =>
        new(MaxHealth, Attack, Defense, Evasion, Math.Clamp(critical, 0, 100), Block, MaxMana);
}
