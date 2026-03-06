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
    public void SetTree()
    {
        isTreeOnTop = true;
        UpdatePathfindingStatus();
    }
    public void SetStructure(int ownerID, int income)
    {
        isStructureOnTop = true;
        UpdatePathfindingStatus();
    }
    public void UpdatePathfindingStatus()
    {
        isWalkable = groundType != GroundType.Water && !isTreeOnTop && !isStructureOnTop && !isUnitOnTop;
        isNavigable = groundType == GroundType.Water && !isTreeOnTop && !isStructureOnTop && !isUnitOnTop;
    }
    public void SetUnitOnTop()
    {
        isUnitOnTop = true;
        UpdatePathfindingStatus();
    }
}