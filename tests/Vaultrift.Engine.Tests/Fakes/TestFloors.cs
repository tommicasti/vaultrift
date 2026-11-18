using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Engine.Tests.Fakes;

public sealed class TestFloors : IFloorProvider
{
    private readonly IReadOnlyList<Func<Floor>> _floors;

    public TestFloors(params Func<Floor>[] floors)
    {
        _floors = floors;
    }

    public int Count => _floors.Count;

    public Floor Create(int number) => _floors[number - 1]();
}