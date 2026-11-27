using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed record LiveChest(int Id, Position Tile, Enemy? Mimic = null);
