namespace Vaultrift.Engine.Randomness;

public sealed class SeededRandom : IRandom
{
    private const ulong Golden = 0x9E3779B97F4A7C15;
    private const double Unit = 1.0 / (1UL << 53);

    public SeededRandom(ulong state)
    {
        State = state;
    }

    public ulong State { get; private set; }

    public static SeededRandom FromSeed(int seed) => new(unchecked((ulong)seed * Golden));

    public double NextDouble() => (Next() >> 11) * Unit;

    private ulong Next()
    {
        unchecked
        {
            State += Golden;
            var z = State;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return z ^ (z >> 31);
        }
    }
}
