using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed record Floor(
    DungeonMap Map,
    Position Start,
    IReadOnlyList<Enemy> Enemies,
    IReadOnlyList<FloorItem> Items,
    Position? Stairs = null);