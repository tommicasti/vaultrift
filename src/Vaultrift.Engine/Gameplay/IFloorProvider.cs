namespace Vaultrift.Engine.Gameplay;

public interface IFloorProvider
{
    int Count { get; }

    Floor Create(int number);
}