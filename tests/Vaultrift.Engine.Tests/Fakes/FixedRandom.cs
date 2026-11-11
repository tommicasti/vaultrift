using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.Tests.Fakes;

public sealed class FixedRandom : IRandom
{
    private readonly Queue<double> _values;

    public FixedRandom(params double[] values)
    {
        _values = new Queue<double>(values);
    }

    public double NextDouble()
    {
        if (_values.Count == 0)
            throw new InvalidOperationException("FixedRandom has no more values.");

        return _values.Dequeue();
    }
}