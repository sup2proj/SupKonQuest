using Enums.Environment;
using Enums.Nature;
using Enums.Structure;
using UnityEngine;

[System.Serializable]
public class TileData
{
    public GroundType groundType;
    public bool isStructureOnTop;
    public bool isUnitOnTop;
    public bool isTreeOnTop;
    public bool isWalkable;
    public bool isNavigable;
    public bool CanPlaceNature;
    public int coordX;
    public int coordY;
    /// <summary>
    /// Crée une nouvelle instance de TileData pour le type de sol donné et les coordonnées.
    /// </summary>
    public TileData(GroundType t,int x,int y)
    {
        groundType = t;
        isStructureOnTop = false;
        isUnitOnTop = false;
        isTreeOnTop = false;
        coordX = x;
        coordY = y;
        isWalkable = (groundType != GroundType.Water);
        isNavigable = (groundType == GroundType.Water);
        CanPlaceNature = true;
    }
    /// <summary>
    /// Marque la tuile comme contenant un arbre et met à jour l'état de pathfinding.
    /// </summary>
    public void SetTree()
    {
        isTreeOnTop = true;
        UpdatePathfindingStatus();
    }
    /// <summary>
    /// Marque la tuile comme contenant une structure et met à jour l'état de pathfinding.
    /// </summary>
    public void SetStructure(int ownerID, int income)
    {
        isStructureOnTop = true;
        UpdatePathfindingStatus();
    }
    /// <summary>
    /// Met à jour les drapeaux isWalkable et isNavigable en fonction des éléments présents sur la tuile.
    /// </summary>
    public void UpdatePathfindingStatus()
    {
        isWalkable = groundType != GroundType.Water && !isTreeOnTop && !isStructureOnTop && !isUnitOnTop;
        isNavigable = groundType == GroundType.Water && !isTreeOnTop && !isStructureOnTop && !isUnitOnTop;
    }
    /// <summary>
    /// Indique qu'une unité occupe la tuile et met à jour l'état de navigation.
    /// </summary>
    public void SetUnitOnTop()
    {
        isUnitOnTop = true;
        UpdatePathfindingStatus();
    }

    /// <summary>
    /// Convertit les coordonnées de la tuile en position monde (Vector3) en utilisant la taille de tuile du map.
    /// </summary>
    public static Vector3 ToWorldPosition(MapGenerator map, TileData tile)
    {
        return new Vector3(tile.coordX * map.tileSize, 0f, tile.coordY * map.tileSize);
    }

    /// <summary>
    /// Calcule la distance au carré sur le plan XZ entre deux positions (ignore Y).
    /// </summary>
    public static float FlatDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    /// <summary>
    /// Indique si la tuile est de l'eau et navigable (pour les unités maritimes).
    /// </summary>
    public static bool IsNavigableWater(TileData tile)
    {
        return tile != null && tile.groundType == GroundType.Water && tile.isNavigable;
    }

    /// <summary>
    /// Indique si la tuile est terrestre et praticable pour les unités au sol.
    /// </summary>
    public static bool IsWalkableLand(TileData tile)
    {
        return tile != null && tile.groundType != GroundType.Water && tile.isWalkable;
    }
}