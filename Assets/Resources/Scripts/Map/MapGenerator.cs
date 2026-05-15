using Enums.Environment;
using Enums.Nature;
using Enums.Structure;
using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    private readonly Color32 _colorGrass = new Color32(10, 170, 0, 255);
    private readonly Color32 _colorDirt = new Color32(170, 160, 0, 255);
    private readonly Color32 _colorSnow = new Color32(255, 255, 255, 255);
    private readonly Color32 _colorWater = new Color32(0, 10, 170, 255);
    public int mapWidth;
    public int mapHeight;
    
    public TileData[,] allTiles;

    // Event déclenché une fois le NavMesh baké et la map prête
    public event System.Action OnMapReady;

    [Header("Source")]
    public GameObject background;
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
    public GameObject StructureCastle;
    public GameObject StructureHarbour;
    public GameObject StructureSpecial;

    [Header("Réglages")]
    public float tileSize = 1f;
    [SerializeField] private NavMeshSurface navMeshSurface;

    private Transform groundFolder;
    private Transform structuresFolder;
    private Transform natureFolder;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        // LoadAndGenerate("EUROPE");
        // LoadAndGenerate("TEST");
        // LoadAndGenerate("LOL");
    }

    public void LoadAndGenerate(string folderName)
    {
        SetupResources();

        // Si la surface n'est pas assignée, on la crée dynamiquement
        if (navMeshSurface == null)
        {
            GameObject navObj = new GameObject("DynamicNavMesh");
            navObj.transform.SetParent(this.transform);
            navMeshSurface = navObj.AddComponent<NavMeshSurface>();

            // FIX : utiliser RenderMeshes plutôt que PhysicsColliders
            // car les MeshColliders des tuiles sont supprimés pour les performances
            navMeshSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            navMeshSurface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
        }

        groundFolder = GetOrCreateFolder("Ground");
        structuresFolder = GetOrCreateFolder("Structures");
        natureFolder = GetOrCreateFolder("Nature");

        string path = "Maps/" + folderName + "/";
        mapLayout = Resources.Load<Texture2D>(path + "MapLayout");
        MapJsonData jsonData = StructureInstance.LoadDataFromPath(path + "MapData");
        mapWidth = mapLayout.width;
        mapHeight = mapLayout.height;
        
        if (mapLayout != null && jsonData != null)
        {
            GenerateWorld();
            PlaceStructures(jsonData);
            AddNature();

            // Bake en dernier, après que toute la géométrie soit en place
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }

            // Notifier que la map est prête (les unités peuvent maintenant spawner)
            OnMapReady?.Invoke();

            Debug.Log($"Monde '{folderName}' généré avec succès !");
        }
        else
        {
            Debug.LogError($"Erreur : Fichiers manquants dans {path}");
        }
    }

    private Transform GetOrCreateFolder(string name)
    {
        Transform existing = transform.Find(name);
        if (existing != null)
            return existing;

        GameObject folder = new GameObject(name);
        folder.transform.SetParent(transform, false);
        return folder.transform;
    }

    void GenerateWorld()
    {
        allTiles = new TileData[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Color32 color = mapLayout.GetPixel(x, y);
                var gData = GetGroundDatas(color);
                allTiles[x, y] = new TileData(gData.type, x, y);

                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject floor = Instantiate(gData.prefab, pos, Quaternion.identity, groundFolder);
                floor.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
                floor.name = $"Tile_{x}_{y}";
                floor.tag = "Ground";
                ConfigureTileNavigation(floor, gData.type);

                // FIX : ne pas détruire le MeshCollider — il est nécessaire pour les raycasts
                // (clic de déplacement des unités) et pour NavMeshSurface en mode PhysicsColliders.
                // Si tu veux absolument les supprimer, garde useGeometry = RenderMeshes ci-dessus.
                // Destroy(floor.GetComponent<MeshCollider>()); // ← ligne supprimée
            }
        }
        Vector3 bgPosition = new Vector3(mapWidth/2, (float)-0.01, mapHeight/2);
        GameObject backGround = Instantiate(background,bgPosition, Quaternion.identity);
        backGround.transform.localScale = new Vector3((mapWidth / 3)*tileSize, 1.5f, (mapHeight / 3)*tileSize);
        backGround.name = "background";

    }

    public bool TryGetTileAtWorldPosition(Vector3 worldPosition, out TileData tile)
    {
        tile = null;

        if (allTiles == null || tileSize <= 0f)
            return false;

        int x = Mathf.RoundToInt(worldPosition.x / tileSize);
        int y = Mathf.RoundToInt(worldPosition.z / tileSize);

        if (x < 0 || y < 0 || x >= allTiles.GetLength(0) || y >= allTiles.GetLength(1))
            return false;

        tile = allTiles[x, y];
        return tile != null;
    }

    private void ConfigureTileNavigation(GameObject tileObject, GroundType type)
    {
        if (tileObject == null)
            return;

        int layer = LayerMask.NameToLayer(type == GroundType.Water ? "Water" : "ground");
        if (layer >= 0)
            SetLayerRecursively(tileObject, layer);

        NavMeshModifier modifier = tileObject.GetComponent<NavMeshModifier>();
        if (modifier == null)
            modifier = tileObject.AddComponent<NavMeshModifier>();

        modifier.overrideArea = true;
        int area = type == GroundType.Water ? NavMesh.GetAreaFromName("Water") : NavMesh.GetAreaFromName("Walkable");
        if (area >= 0)
            modifier.area = area;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    void PlaceStructures(MapJsonData data)
    {
        SpawnStructureGroup(data.startPoints, StructureCastle, StructureType.Structure, 1);
        SpawnStructureGroup(data.castles, StructureCastle, StructureType.Structure, 1);
        SpawnStructureGroup(data.harbours, StructureHarbour, StructureType.Harbour, 1);
        SpawnStructureGroup(data.special, StructureSpecial, StructureType.NeutralStructure, 1);
    }

    void SpawnStructureGroup(List<PointData> points, GameObject prefab, StructureType type, int income)
    {
        if (points == null || prefab == null) return;
        int h = mapLayout.height;

        foreach (PointData p in points)
        {
            if (p.x >= 0 && p.x < allTiles.GetLength(0) && p.y >= 0 && p.y < allTiles.GetLength(1))
            {
                int unityY = (mapHeight - 1 - p.y) ;
                Vector3 pos = new Vector3((p.x) * tileSize, 0, unityY* tileSize);
                
                GameObject structureObj = Instantiate(prefab, pos, Quaternion.identity, structuresFolder);
                structureObj.transform.localScale = new Vector3(3f, 3f, 3f);
                structureObj.name = $"{type}_{p.x}_{p.y}";

                var si = structureObj.GetComponent<StructureInstance>();
                if (si != null)
                {
                    si.InitializePlayerId(p.owner);
                    si.structureType = type;
                    si.neutralStructure = (type == StructureType.NeutralStructure);
                    BlockNature(p.x, unityY);
                }
                else
                {
                    Debug.LogError($"Prefab {prefab.name} sans StructureInstance");
                }
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
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tile = allTiles[x, y];

                if (tile.CanPlaceNature && tile.groundType != GroundType.Water)
                {
                    if (Random.value < 0.2f)
                    {
                        GameObject treeObj = null;
                        if (tile.groundType == GroundType.Grass)
                            treeObj = treeGrass;
                        else if (tile.groundType == GroundType.Dirt)
                            treeObj = treeDirt;
                        else if (tile.groundType == GroundType.Snow)
                            treeObj = treeSnow;

                        if (treeObj != null)
                        {
                            Vector3 pos = new Vector3((x) * tileSize, 0, y);
                            
                            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            GameObject tree = Instantiate(treeObj, pos, rot, natureFolder);
                            tree.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                            tree.name = $"Tree_{x}_{y}";
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
        string StructurePath = "Prefabs/Structures/";
        
        if (background == null) background = Resources.Load<GameObject>(environmentPath+"background");

        if (groundDirt == null) groundDirt = Resources.Load<GameObject>(environmentPath+"Env_Ground_Dirt");
        if (groundGrass == null) groundGrass = Resources.Load<GameObject>(environmentPath+"Env_Ground_Grass");
        if (groundSnow == null) groundSnow = Resources.Load<GameObject>(environmentPath+"Env_Ground_Snow");
        if (groundWater == null) groundWater = Resources.Load<GameObject>(environmentPath+"Env_Ground_Water");

        if (treeDirt == null) treeDirt = Resources.Load<GameObject>(naturePath + "Nature_Tree_Dirt");
        if (treeGrass == null) treeGrass = Resources.Load<GameObject>(naturePath + "Nature_Tree_Grass");
        if (treeSnow == null) treeSnow = Resources.Load<GameObject>(naturePath + "Nature_Tree_Snow");

        if (StructureCastle == null) StructureCastle = Resources.Load<GameObject>(StructurePath + "Structure");
        if (StructureHarbour == null) StructureHarbour = Resources.Load<GameObject>(StructurePath + "Harbour");
        if (StructureSpecial == null) StructureSpecial = Resources.Load<GameObject>(StructurePath + "NeutralStructure");
    }
}
