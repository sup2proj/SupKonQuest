using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    private readonly Color32 _colorGrass = new Color32(10, 170, 0, 255);
    private readonly Color32 _colorDirt = new Color32(170, 160, 0, 255);
    private readonly Color32 _colorSnow = new Color32(255, 255, 255, 255);
    private readonly Color32 _colorWater = new Color32(0, 10, 170, 255);
    
    public TileData[,] allTiles;

    [Header("Source")]
    public Texture2D mapLayout;

    [Header("Grounds")]
    public GameObject groundDirt;
    public GameObject groundGrass;
    public GameObject groundSnow;
    public GameObject groundWater;
    
    [Header("Trees")]
    public GameObject treeDirt;
    public GameObject treeGrass;
    public GameObject treeSnow;
    
    [Header("Buildings")]
    public GameObject buildingCastle;
    public GameObject buildingArsenal;
    public GameObject buildingSpecial;

    [Header("Réglages")]
    public float tileSize = 1f;

    void Start()
    {
        // LoadAndGenerate("EUROPE");
        LoadAndGenerate("TEST");
        // LoadAndGenerate("LOL");
    }

    void LoadAndGenerate(string folderName)
    {
        SetupResources();
        string path = "Maps/" + folderName + "/";
        mapLayout = Resources.Load<Texture2D>(path + "MapLayout");
        MapJsonData jsonData = BuildingData.LoadDataFromPath(path + "MapData");

        if (mapLayout != null && jsonData != null)
        {
            GenerateWorld();      
            PlaceBuildings(jsonData); 
            AddTrees();     
            Debug.Log($"Monde '{folderName}' généré avec succès !");
        }
        else
        {
            Debug.LogError($"Erreur : Fichiers manquants dans {path}");
        }
    }

    void GenerateWorld()
    {
        int w = mapLayout.width;
        int h = mapLayout.height;
        allTiles = new TileData[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color32 color = mapLayout.GetPixel(x, y);
                var gData = GetGroundDatas(color);
                int arrayY = h - 1 - y;
                allTiles[x, arrayY] = new TileData(gData.type, x, arrayY);

                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject floor = Instantiate(gData.prefab, pos, Quaternion.identity, transform);
                floor.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
                floor.name = $"Tile_{x}_{arrayY}";
            }
        }
    }

    void PlaceBuildings(MapJsonData data)
    {
        SpawnBuildingGroup(data.startPoints, buildingCastle, TileData.BuildingType.Castle);
        SpawnBuildingGroup(data.castles, buildingCastle, TileData.BuildingType.Castle);
        SpawnBuildingGroup(data.arsenals, buildingArsenal, TileData.BuildingType.Arsenal);
        SpawnBuildingGroup(data.special, buildingSpecial, TileData.BuildingType.Special);
    }

    void SpawnBuildingGroup(List<PointData> points, GameObject prefab, TileData.BuildingType type)
    {
        if (points == null || prefab == null) return;
        int h = mapLayout.height;

        foreach (PointData p in points)
        {
            if (p.x >= 0 && p.x < allTiles.GetLength(0) && p.y >= 0 && p.y < allTiles.GetLength(1))
            {
                float unityZ = (h - 1 - p.y) * tileSize;
                Vector3 pos = new Vector3(p.x * tileSize, 0, unityZ);
                
                GameObject bObj = Instantiate(prefab, pos, Quaternion.identity, transform);
                bObj.transform.localScale = new Vector3(3f, 3f, 3f);
                bObj.name = $"{type}_{p.x}_{p.y}";

                allTiles[p.x, p.y].SetBuilding(type, bObj);
                allTiles[p.x, p.y].SetOwnerID(p.owner);
            }
        }
    }

    void AddTrees()
    {
        int h = mapLayout.height;
        for (int x = 0; x < allTiles.GetLength(0); x++)
        {
            for (int y = 0; y < allTiles.GetLength(1); y++)
            {
                TileData tile = allTiles[x, y];

                if (tile.buildingType != TileData.BuildingType.None || tile.groundType == TileData.GroundType.Water)
                    continue;

                if (Random.value < 0.3f)
                {
                    GameObject treePrefab = null;
                    TileData.TreeType tType = TileData.TreeType.None;

                    if (tile.groundType == TileData.GroundType.Grass) { treePrefab = treeGrass; tType = TileData.TreeType.Grass; }
                    else if (tile.groundType == TileData.GroundType.Dirt) { treePrefab = treeDirt; tType = TileData.TreeType.Dirt; }
                    else if (tile.groundType == TileData.GroundType.Snow) { treePrefab = treeSnow; tType = TileData.TreeType.Snow; }

                    if (treePrefab != null)
                    {
                        float unityZ = (h - 1 - y) * tileSize;
                        Vector3 pos = new Vector3(x * tileSize, 0, unityZ);
                        
                        Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        Instantiate(treePrefab, pos, rot, transform);
                        treePrefab.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

                        tile.SetTree(tType);
                    }
                }
            }
        }
    }

    bool IsColor(Color32 c1, Color32 c2)
    {
        return c1.r == c2.r && c1.g == c2.g && c1.b == c2.b;
    }

    (TileData.GroundType type, GameObject prefab) GetGroundDatas(Color32 c)
    {
        if (IsColor(c, _colorWater)) return (TileData.GroundType.Water, groundWater);
        if (IsColor(c, _colorDirt)) return (TileData.GroundType.Dirt, groundDirt);
        if (IsColor(c, _colorSnow)) return (TileData.GroundType.Snow, groundSnow);
        return (TileData.GroundType.Grass, groundGrass);
    }
    
    void SetupResources()
    {
        string environmentPath = "Prefabs/Environment/";
        string naturePath = "Prefabs/Nature/";
        string buildingPath = "Prefabs/Buildings/";
        
        if (groundDirt == null) groundDirt = Resources.Load<GameObject>(environmentPath+"Env_Ground_Dirt");
        if (groundGrass == null) groundGrass = Resources.Load<GameObject>(environmentPath+"Env_Ground_Grass");
        if (groundSnow == null) groundSnow = Resources.Load<GameObject>(environmentPath+"Env_Ground_Snow");
        if (groundWater == null) groundWater = Resources.Load<GameObject>(environmentPath+"Env_Ground_Water");

        if (treeDirt == null) treeDirt = Resources.Load<GameObject>(naturePath+"Nature_Tree_Dirt");
        if (treeGrass == null) treeGrass = Resources.Load<GameObject>(naturePath+"Nature_Tree_Grass");
        if (treeSnow == null) treeSnow = Resources.Load<GameObject>(naturePath+"Nature_Tree_Snow");

        if (buildingCastle == null) buildingCastle = Resources.Load<GameObject>(buildingPath+"Building_Castle");
        if (buildingArsenal == null) buildingArsenal = Resources.Load<GameObject>(buildingPath+"Building_Arsenal");
        if (buildingSpecial == null) buildingSpecial = Resources.Load<GameObject>(buildingPath+"Building_Special");
    }
}