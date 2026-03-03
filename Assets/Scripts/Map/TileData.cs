using UnityEngine;

[System.Serializable]
public class TileData
{
    public enum GroundType { Grass, Dirt, Snow, Water }
    public enum TreeType { None, Grass, Dirt, Snow }
    public enum BuildingType { None, Castle, Special, Arsenal }
    public GroundType groundType;
    public TreeType treeType;
    public BuildingType buildingType;
    public bool hasTree;
    public bool isWalkable;
    public bool isNavigable;
    public GameObject unitOnTop;
    public GameObject buildingOnTop;
    public int coordX;
    public int coordY;
    public int currentOwnerID;
    public TileData(GroundType t,int x,int y)
    {
        this.groundType = t;
        this.treeType = TreeType.None;
        this.buildingType = BuildingType.None;
        this.hasTree = false;
        this.unitOnTop = null;
        this.buildingOnTop = null;
        this.coordX = x;
        this.coordY = y;
        this.currentOwnerID = -1;
        this.isWalkable = (this.groundType != GroundType.Water);
        this.isNavigable = (this.groundType == GroundType.Water);
    }
    public void SetTree(TreeType type)
    {
        this.treeType = type;
        this.hasTree = true;
        UpdatePathfindingStatus();
    }
    public void SetBuilding(BuildingType type, GameObject buildingObj)
    {
        this.buildingType = type;
        this.buildingOnTop = buildingObj;
        UpdatePathfindingStatus();
    }
    public void UpdatePathfindingStatus()
    {
        this.isWalkable = (this.groundType != GroundType.Water && !this.hasTree && this.buildingType == BuildingType.None && this.unitOnTop == null);
        this.isNavigable = (this.groundType == GroundType.Water && this.unitOnTop == null);
    }
    public void SetUnitOnTop(GameObject unit)
    {
        this.unitOnTop = unit;
        UpdatePathfindingStatus();
    }
    public void SetOwnerID(int ownerID)
    {
        this.currentOwnerID = ownerID;
    }
}