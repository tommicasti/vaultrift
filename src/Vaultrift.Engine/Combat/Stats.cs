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
}