namespace Vaultrift.Engine.World;

public sealed record FloorLayout(DungeonMap Map, IReadOnlyList<Room> Rooms, Room StartRoom, Room BossRoom);