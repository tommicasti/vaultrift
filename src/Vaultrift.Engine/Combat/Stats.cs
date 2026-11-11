namespace Vaultrift.Engine.Combat;

public sealed record Stats
{
    public Stats(int maxHealth, int attack, int defense)
    {
        if (maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be positive.");

        if (attack < 0)
            throw new ArgumentOutOfRangeException(nameof(attack), attack, "Attack cannot be negative.");

        if (defense < 0)
            throw new ArgumentOutOfRangeException(nameof(defense), defense, "Defense cannot be negative.");

        MaxHealth = maxHealth;
        Attack = attack;
        Defense = defense;
    }

    public int MaxHealth { get; }

    public int Attack { get; }

    public int Defense { get; }
}