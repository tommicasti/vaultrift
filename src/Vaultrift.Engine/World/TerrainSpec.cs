namespace Vaultrift.Engine.World;

public sealed record TerrainSpec(TileType Tile, double Density)
{
    public const int MinPoolSize = 3;
    public const int MaxPoolSize = 7;
}
