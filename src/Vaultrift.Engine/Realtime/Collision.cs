using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public static class Collision
{
    public static Position TileOf(Vector point) =>
        new((int)Math.Floor(point.X + 0.5), (int)Math.Floor(point.Y + 0.5));

    public static bool HitsWall(DungeonMap map, Vector center, double radius, bool passesThroughWalls = false)
    {
        ArgumentNullException.ThrowIfNull(map);

        var fromX = (int)Math.Floor(center.X - radius + 0.5);
        var toX = (int)Math.Floor(center.X + radius + 0.5);
        var fromY = (int)Math.Floor(center.Y - radius + 0.5);
        var toY = (int)Math.Floor(center.Y + radius + 0.5);

        for (var y = fromY; y <= toY; y++)
        {
            for (var x = fromX; x <= toX; x++)
            {
                var tile = new Position(x, y);
                var blocked = passesThroughWalls ? !map.IsInside(tile) : !map.IsWalkable(tile);

                if (blocked && Overlaps(center, radius, tile))
                    return true;
            }
        }

        return false;
    }

    public static Vector Slide(
        DungeonMap map,
        Vector from,
        Vector step,
        double radius,
        Func<Vector, bool>? occupied = null,
        bool passesThroughWalls = false,
        bool roundsCorners = false)
    {
        bool Blocked(Vector point) => HitsWall(map, point, radius, passesThroughWalls) || (occupied?.Invoke(point) ?? false);

        var position = from;
        var alongX = position with { X = position.X + step.X };

        if (step.X != 0 && !Blocked(alongX))
            position = alongX;
        else if (roundsCorners && step.X != 0 && step.Y == 0)
            position = RoundCorner(map, position, Math.Sign(step.X), 0, Math.Abs(step.X), Blocked);

        var alongY = position with { Y = position.Y + step.Y };

        if (step.Y != 0 && !Blocked(alongY))
            position = alongY;
        else if (roundsCorners && step.Y != 0 && step.X == 0)
            position = RoundCorner(map, position, 0, Math.Sign(step.Y), Math.Abs(step.Y), Blocked);

        return position;
    }

    private static Vector RoundCorner(DungeonMap map, Vector position, int dx, int dy, double distance, Func<Vector, bool> blocked)
    {
        var tile = TileOf(position);

        if (!map.IsWalkable(new Position(tile.X + dx, tile.Y + dy)))
            return position;

        var offset = dx != 0 ? tile.Y - position.Y : tile.X - position.X;

        if (Math.Abs(offset) < 1e-6)
            return position;

        var shift = Math.Sign(offset) * Math.Min(distance, Math.Abs(offset));
        var nudged = dx != 0 ? position with { Y = position.Y + shift } : position with { X = position.X + shift };

        return blocked(nudged) ? position : nudged;
    }

    public static bool ClearPath(DungeonMap map, Vector from, Vector to, double radius, double spacing = 0.2)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spacing);

        var offset = to - from;
        var steps = Math.Max(1, (int)Math.Ceiling(offset.Length / spacing));

        for (var step = 1; step <= steps; step++)
        {
            if (HitsWall(map, from + offset * ((double)step / steps), radius))
                return false;
        }

        return true;
    }

    private static bool Overlaps(Vector center, double radius, Position tile)
    {
        var nearestX = Math.Clamp(center.X, tile.X - 0.5, tile.X + 0.5);
        var nearestY = Math.Clamp(center.Y, tile.Y - 0.5, tile.Y + 0.5);
        var dx = center.X - nearestX;
        var dy = center.Y - nearestY;

        return dx * dx + dy * dy < radius * radius;
    }
}
