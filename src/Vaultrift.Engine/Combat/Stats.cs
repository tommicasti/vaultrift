using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Combat;

public sealed record Stats
{
    public Stats(int maxHealth, int attack, int defense, int evasion = 0)
    {
        if (maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be positive.");

        if (attack < 0)
            throw new ArgumentOutOfRangeException(nameof(attack), attack, "Attack cannot be negative.");

        if (defense < 0)
            throw new ArgumentOutOfRangeException(nameof(defense), defense, "Defense cannot be negative.");

        if (evasion is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(evasion), evasion, "Evasion must be between 0 and 100.");

        MaxHealth = maxHealth;
        Attack = attack;
        Defense = defense;
        Evasion = evasion;
    }

    public int MaxHealth { get; }

    public int Attack { get; }

    public int Defense { get; }

    public int Evasion { get; }

    public Stats Apply(IEnumerable<StatModifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var attack = Attack;
        var defense = Defense;
        var evasion = Evasion;

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
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifiers), modifier.Stat, "Unknown stat type.");
            }
        }

        return new Stats(MaxHealth, Math.Max(0, attack), Math.Max(0, defense), Math.Clamp(evasion, 0, 100));
    }
}