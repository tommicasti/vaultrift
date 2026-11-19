namespace Vaultrift.Engine.World;

public sealed record TerrainSpec(TileType Tile, double Density, TerrainShape Shape = TerrainShape.Pool)
{
    public const int MinPoolSize = 3;
    public const int MaxPoolSize = 7;
    public const int MinClusterSize = 1;
    public const int MaxClusterSize = 3;
    public const int MinStreamLength = 3;
}
