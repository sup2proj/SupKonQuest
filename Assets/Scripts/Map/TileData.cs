using Enums.Environment;
using Enums.Nature;
using Enums.Structure;
using UnityEngine;

[System.Serializable]
public class TileData
{
    public GroundType groundType;
    public bool isBuildingOnTop;
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
        isBuildingOnTop = false;
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
    public void SetBuilding(int ownerID, int income)
    {
        isBuildingOnTop = true;
        UpdatePathfindingStatus();
    }
    public void UpdatePathfindingStatus()
    {
        isWalkable = groundType != GroundType.Water && !isTreeOnTop && !isBuildingOnTop && !isUnitOnTop;
        isNavigable = groundType == GroundType.Water && !isTreeOnTop && !isBuildingOnTop && !isUnitOnTop;
    }
    public void SetUnitOnTop()
    {
        isUnitOnTop = true;
        UpdatePathfindingStatus();
    }
}