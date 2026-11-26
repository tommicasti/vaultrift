using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed class FlowField
{
    public const int Unreachable = int.MaxValue;

    private static readonly Position[] Neighbours = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];

    private readonly int[,] _distance;

    private FlowField(Position target, int[,] distance)
    {
        Target = target;
        _distance = distance;
    }

    public Position Target { get; }

    public static FlowField Toward(DungeonMap map, Position target)
    {
        ArgumentNullException.ThrowIfNull(map);

        var distance = new int[map.Height, map.Width];

        for (var y = 0; y < map.Height; y++)
            for (var x = 0; x < map.Width; x++)
                distance[y, x] = Unreachable;

        if (!map.IsWalkable(target))
            return new FlowField(target, distance);

        var queue = new Queue<Position>();
        distance[target.Y, target.X] = 0;
        queue.Enqueue(target);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var next = distance[current.Y, current.X] + 1;

            foreach (var offset in Neighbours)
            {
                var neighbour = new Position(current.X + offset.X, current.Y + offset.Y);

                if (!map.IsWalkable(neighbour) || distance[neighbour.Y, neighbour.X] != Unreachable)
                    continue;

                distance[neighbour.Y, neighbour.X] = next;
                queue.Enqueue(neighbour);
            }
        }

        return new FlowField(target, distance);
    }

    public int DistanceFrom(Position tile) =>
        tile.Y >= 0 && tile.Y < _distance.GetLength(0) && tile.X >= 0 && tile.X < _distance.GetLength(1)
            ? _distance[tile.Y, tile.X]
            : Unreachable;

    public Position? NextStep(Position from)
    {
        var best = DistanceFrom(from);
        Position? step = null;

        foreach (var offset in Neighbours)
        {
            var neighbour = new Position(from.X + offset.X, from.Y + offset.Y);
            var distance = DistanceFrom(neighbour);

            if (distance < best)
            {
                best = distance;
                step = neighbour;
            }
        }

        return step;
    }
}
