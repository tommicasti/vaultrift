namespace Vaultrift.Engine.Tests.Support;

public static class Dice
{
    public const double Fails = 0.99;

    public static double[] AlwaysFail(int count = 30) => Enumerable.Repeat(Fails, count).ToArray();

    public static double[] Then(double[] first, int failures = 30) => [.. first, .. AlwaysFail(failures)];
}
