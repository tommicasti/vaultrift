using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed record LiveItem(int Id, Position Tile, ItemInstance Item);
