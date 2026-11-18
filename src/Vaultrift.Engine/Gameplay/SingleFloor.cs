using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class SingleFloor : IFloorProvider
{
    private readonly Floor _floor;

    public SingleFloor(Floor floor)
    {
        ArgumentNullException.ThrowIfNull(floor);
        _floor = floor;
    }

    public int Count => 1;

    public Floor Create(int number) =>
        number == 1 ? _floor : throw new ArgumentOutOfRangeException(nameof(number), number, "There is only one floor.");

    public static SingleFloor From(DungeonMap map, Player player, IEnumerable<Enemy> enemies, IEnumerable<FloorItem>? items)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(enemies);

        return new SingleFloor(new Floor(map, player.Position, enemies.ToList(), items?.ToList() ?? []));
    }
}