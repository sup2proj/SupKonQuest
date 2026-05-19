using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoLauncher : MonoBehaviour
{
    public static AutoLauncher Instance { get; private set; }

    private static bool launchRequested;
    private static string pendingMapFolder = "TEST";
    private static int pendingAiCount = 1;
    private static int pendingAiDifficulty = 2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Request(string folder, int aiCount = 1, int aiDifficulty = 2)
    {
        pendingMapFolder = string.IsNullOrWhiteSpace(folder) ? "TEST" : folder;
        pendingAiCount = Mathf.Max(0, aiCount);
        pendingAiDifficulty = Mathf.Clamp(aiDifficulty, 1, 2);
        launchRequested = true;

        if (Instance == null)
        {
            GameObject go = new GameObject("AutoLauncher");
            go.AddComponent<AutoLauncher>();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!launchRequested || scene.name != "Game")
            return;

        launchRequested = false;
        CreateGame(pendingMapFolder, pendingAiCount, pendingAiDifficulty);
    }

    /// <summary>
    /// Crée et lance la partie : génère la map, configure les camps et la caméra.
    /// </summary>
    public void CreateGame(string mapFolderName = "TEST", int aiCount = 1, int aiDifficulty = 2)
    {
        Debug.Log($"AutoLauncher.CreateGame called with mapFolderName='{mapFolderName}', aiCount={aiCount}, aiDifficulty={aiDifficulty}");
        string localPlayerName = "toto";
        string[] playerList = { "toto" };

        StructureAttribution campAssignment = PrepareStructureAttribution(mapFolderName, aiCount);
        LogStartPointOwnersAfterAttribution();

        MapGenerator mapGenerator = CreateFreshMapGeneratorAndGenerate(mapFolderName);

        campAssignment.SetCampAssignment(playerList);
        (int startCameraPositionX, int startCameraPositionY) = campAssignment.GetPlayerCameraStartPosition(localPlayerName);

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = Object.FindObjectOfType<Camera>();
        }

        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        else
        {
            mainCam.gameObject.name = "Main Camera";
            mainCam.gameObject.tag = "MainCamera";
        }

        CameraMouvement camMovement = mainCam.GetComponent<CameraMouvement>();
        if (camMovement == null)
            camMovement = mainCam.gameObject.AddComponent<CameraMouvement>();

        camMovement.SetUpCamera(mapGenerator.mapWidth, mapGenerator.mapHeight, startCameraPositionX, startCameraPositionY);

        InstantiateAIs(aiCount, aiDifficulty);
    }

    private StructureAttribution PrepareStructureAttribution(string mapFolderName, int aiCount)
    {
        var campAssignment = new StructureAttribution();
        campAssignment.Setup(mapFolderName);

        int totalPlayers = 1 + aiCount;
        campAssignment.AssignRandomOwners(totalPlayers);
        Debug.Log($"[AutoLauncher] Structures attribuees aleatoirement pour {totalPlayers} joueurs.");

        return campAssignment;
    }

    private void LogStartPointOwnersAfterAttribution()
    {
        MapJsonData modifiedData = StructureAttribution.LastModifiedJsonData;
        if (modifiedData == null || modifiedData.startPoints == null)
            return;

        Debug.Log("[AutoLauncher] Owners des startPoints apres attribution:");
        for (int i = 0; i < modifiedData.startPoints.Count; i++)
        {
            Debug.Log($"  StartPoint {i}: owner = {modifiedData.startPoints[i].owner}");
        }
    }

    private MapGenerator CreateFreshMapGeneratorAndGenerate(string mapFolderName)
    {
        MapGenerator mapGenerator = Object.FindAnyObjectByType<MapGenerator>();

        if (mapGenerator != null)
        {
            Debug.Log("AutoLauncher: Found existing MapGenerator in scene - replacing it to apply new owners.");
            Object.DestroyImmediate(mapGenerator.gameObject);
        }

        ManagerController.initializePermanentGameObject();

        GameObject mapContainer = new GameObject("MAP);
        mapGenerator = mapContainer.AddComponent<MapGenerator>();
        mapGenerator.LoadAndGenerate(mapFolderName);
        return mapGenerator;
    }

    private void InstantiateAIs(int aiCount, int aiDifficulty)
    {
        IAInstance[] existingAis = Object.FindObjectsByType<IAInstance>(FindObjectsSortMode.None);
        for (int i = 0; i < existingAis.Length; i++)
        {
            Object.Destroy(existingAis[i].gameObject);
        }

        int safeAiCount = Mathf.Max(0, aiCount);
        int safeDifficulty = Mathf.Clamp(aiDifficulty, 1, 2);
        for (int i = 0; i < safeAiCount; i++)
        {
            int aiPlayerId = i + 2;
            GameObject aiGO = new GameObject($"IAInstance_Player{aiPlayerId}");
            IAInstance ia = aiGO.AddComponent<IAInstance>();
            ia.Configure(aiPlayerId, safeDifficulty);
        }

        Debug.Log($"[AutoLauncher] Instantiated {safeAiCount} IA instances (difficulty={safeDifficulty}).");
    }
}

