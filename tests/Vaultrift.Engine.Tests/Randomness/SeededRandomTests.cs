using Vaultrift.Engine.Randomness;

namespace Vaultrift.Engine.Tests.Randomness;

public class SeededRandomTests
{
    private static List<double> Draw(IRandom random, int count) =>
        Enumerable.Range(0, count).Select(_ => random.NextDouble()).ToList();

    [Fact]
    public void TheSameSeed_GivesTheSameNumbers()
    {
        Assert.Equal(Draw(SeededRandom.FromSeed(42), 20), Draw(SeededRandom.FromSeed(42), 20));
        Assert.NotEqual(Draw(SeededRandom.FromSeed(42), 20), Draw(SeededRandom.FromSeed(43), 20));
    }

    [Fact]
    public void ResumingFromTheState_ContinuesTheSameSequence()
    {
        var original = SeededRandom.FromSeed(7);
        Draw(original, 50);
        var resumed = new SeededRandom(original.State);

        Assert.Equal(Draw(original, 30), Draw(resumed, 30));
    }

    [Fact]
    public void Numbers_StayBetweenZeroAndOne_AndSpreadEvenly()
    {
        var numbers = Draw(SeededRandom.FromSeed(1), 20_000);

        Assert.All(numbers, number => Assert.InRange(number, 0.0, 0.9999999999));
        Assert.InRange(numbers.Average(), 0.49, 0.51);
        Assert.InRange(numbers.Count(number => number < 0.1), 1_800, 2_200);
    }
}
