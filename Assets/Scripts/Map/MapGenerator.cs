using Enums.Environment;
using Enums.Nature;
using Enums.Structure;
using UnityEngine;
using System.Collections.Generic;
using Building;

public class MapGenerator : MonoBehaviour
{
    private readonly Color32 _colorGrass = new Color32(10, 170, 0, 255);
    private readonly Color32 _colorDirt = new Color32(170, 160, 0, 255);
    private readonly Color32 _colorSnow = new Color32(255, 255, 255, 255);
    private readonly Color32 _colorWater = new Color32(0, 10, 170, 255);
    public GroundType groundType;
    public TreeType treeType;
    
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
    
    [Header("Structures")]
    public GameObject buildingCastle;
    public GameObject buildingHarbour;
    public GameObject buildingSpecial;

    [Header("Réglages")]
    public float tileSize = 1f;

    void Start()
    {
        // LoadAndGenerate("EUROPE");
        // LoadAndGenerate("TEST");
        // LoadAndGenerate("LOL");
    }

    public void LoadAndGenerate(string folderName)
    {
        SetupResources();
        string path = "Maps/" + folderName + "/";
        mapLayout = Resources.Load<Texture2D>(path + "MapLayout");
        MapJsonData jsonData = BuildingData.LoadDataFromPath(path + "MapData");

        if (mapLayout != null && jsonData != null)
        {
            GenerateWorld();      
            PlaceBuildings(jsonData); 
            AddNature();     
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
                allTiles[x, y] = new TileData(gData.type, x, y);

                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject floor = Instantiate(gData.prefab, pos, Quaternion.identity, transform);
                floor.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
                floor.name = $"Tile_{x}_{y}";
            }
        }
    }

    void PlaceBuildings(MapJsonData data)
    {
        SpawnBuildingGroup(data.startPoints, buildingCastle, StructureType.Structure, 1);
        SpawnBuildingGroup(data.castles, buildingCastle,  StructureType.Structure, 1);
        SpawnBuildingGroup(data.harbours, buildingHarbour,  StructureType.Harbour, 1);
        SpawnBuildingGroup(data.special, buildingSpecial,  StructureType.NeutralStructure, 1);
    }

    void SpawnBuildingGroup(List<PointData> points, GameObject prefab,  StructureType type, int income)
    {
        if (points == null || prefab == null) return;
        int h = mapLayout.height;

        foreach (PointData p in points)
        {
            if (p.x >= 0 && p.x < allTiles.GetLength(0) && p.y >= 0 && p.y < allTiles.GetLength(1))
            {
                int unityY = (h - 1 - p.y) ;
                Vector3 pos = new Vector3(p.x * tileSize, 0, unityY* tileSize);
                
                GameObject buildingObj = Instantiate(prefab, pos, Quaternion.identity, transform);
                buildingObj.transform.localScale = new Vector3(3f, 3f, 3f);
                buildingObj.name = $"{type}_{p.x}_{p.y}";
                BuildingController buildingController = buildingObj.AddComponent<BuildingController>();
                buildingController.Init(p.x, p.y,-1,income);

                BlockNature(p.x, unityY);
            }
        }
    }

    void BlockNature(int x, int y)
    {
        int voisinX = 0;
        int voisinY = 0;
        
        for (int i = -3; i < 4; i++)
        {
            for (int j = -3; j < 4; j++)
            {
                voisinX = x + i;
                voisinY = y + j;
                if (voisinX >= 0 && voisinX < allTiles.GetLength(0) && voisinY >= 0 && voisinY < allTiles.GetLength(1))
                {
                    allTiles[voisinX, voisinY].CanPlaceNature = false;
                }
            }
        }
    }
    
    void AddNature()
    {
        int h = mapLayout.height;
        for (int x = 0; x < allTiles.GetLength(0); x++)
        {
            for (int y = 0; y < allTiles.GetLength(1); y++)
            {
                TileData tile = allTiles[x, y];

                if (tile.CanPlaceNature && tile.groundType != GroundType.Water)
                {
                    if (Random.value < 0.2f)
                    {
                        GameObject treeObj = null;
                        TreeType tType = TreeType.None;

                        if (tile.groundType == GroundType.Grass) { treeObj = treeGrass; tType = TreeType.Grass; }
                        else if (tile.groundType == GroundType.Dirt) { treeObj = treeDirt; tType = TreeType.Dirt; }
                        else if (tile.groundType == GroundType.Snow) { treeObj = treeSnow; tType = TreeType.Snow; }

                        if (treeObj != null)
                        {
                            Vector3 pos = new Vector3(x * tileSize, 0, y);
                            
                            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            Instantiate(treeObj, pos, rot, transform);
                            treeObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                            treeObj.name = $"Tree_{x}_{y}";
                            tile.SetTree();
                        }
                    }
                }
            }
        }
    }

    bool IsColor(Color32 c1, Color32 c2)
    {
        return c1.r == c2.r && c1.g == c2.g && c1.b == c2.b;
    }

    (GroundType type, GameObject prefab) GetGroundDatas(Color32 c)
    {
        if (IsColor(c, _colorWater)) return (GroundType.Water, groundWater);
        if (IsColor(c, _colorDirt)) return (GroundType.Dirt, groundDirt);
        if (IsColor(c, _colorSnow)) return (GroundType.Snow, groundSnow);
        return (GroundType.Grass, groundGrass);
    }
    
    void SetupResources()
    {
        string environmentPath = "Prefabs/Environment/";
        string naturePath = "Prefabs/Nature/";
        string buildingPath = "Prefabs/Structures/";
        
        if (groundDirt == null) groundDirt = Resources.Load<GameObject>(environmentPath+"Env_Ground_Dirt");
        if (groundGrass == null) groundGrass = Resources.Load<GameObject>(environmentPath+"Env_Ground_Grass");
        if (groundSnow == null) groundSnow = Resources.Load<GameObject>(environmentPath+"Env_Ground_Snow");
        if (groundWater == null) groundWater = Resources.Load<GameObject>(environmentPath+"Env_Ground_Water");

        if (treeDirt == null) treeDirt = Resources.Load<GameObject>(naturePath+"Nature_Tree_Dirt");
        if (treeGrass == null) treeGrass = Resources.Load<GameObject>(naturePath+"Nature_Tree_Grass");
        if (treeSnow == null) treeSnow = Resources.Load<GameObject>(naturePath+"Nature_Tree_Snow");

        if (buildingCastle == null) buildingCastle = Resources.Load<GameObject>(buildingPath+"Structure");
        if (buildingHarbour == null) buildingHarbour = Resources.Load<GameObject>(buildingPath+"Harbour");
        if (buildingSpecial == null) buildingSpecial = Resources.Load<GameObject>(buildingPath+"NeutralStructure");
    }
}