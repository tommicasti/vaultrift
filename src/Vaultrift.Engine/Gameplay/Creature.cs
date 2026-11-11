using Vaultrift.Engine.Combat;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public abstract class Creature
{
    protected Creature(Position position, Stats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        Position = position;
        Stats = stats;
        Health = stats.MaxHealth;
    }

    public Position Position { get; internal set; }

    public Stats Stats { get; }

    public int Health { get; private set; }

    public bool IsDead => Health == 0;

    internal void TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Damage cannot be negative.");

        Health = Math.Max(0, Health - amount);
    }
}