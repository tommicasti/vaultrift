namespace Vaultrift.Engine.Randomness;

public static class RandomExtensions
{
    public static int Next(this IRandom random, int minInclusive, int maxExclusive)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "The range must not be empty.");

        var value = minInclusive + (int)(random.NextDouble() * (maxExclusive - minInclusive));

        return Math.Min(value, maxExclusive - 1);
    }

    public static T Pick<T>(this IRandom random, IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            throw new ArgumentException("Cannot pick from an empty list.", nameof(items));

        return items[random.Next(0, items.Count)];
    }
}