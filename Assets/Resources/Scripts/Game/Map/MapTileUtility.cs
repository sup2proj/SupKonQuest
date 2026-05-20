using Enums.Environment;
using UnityEngine;

public static class MapTileUtility
{
    public static Vector3 TileToWorldPosition(MapGenerator map, TileData tile)
    {
        return new Vector3(tile.coordX * map.tileSize, 0f, tile.coordY * map.tileSize);
    }

    public static float FlatDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    public static bool IsNavigableWaterTile(TileData tile)
    {
        return tile != null && tile.groundType == GroundType.Water && tile.isNavigable;
    }

    public static bool IsWalkableLandTile(TileData tile)
    {
        return tile != null && tile.groundType != GroundType.Water && tile.isWalkable;
    }
}
