namespace Vaultrift.Engine.Randomness;

public sealed class SystemRandom : IRandom
{
    private readonly Random _random;

    public SystemRandom(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public double NextDouble() => _random.NextDouble();
}