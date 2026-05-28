using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoLauncher : MonoBehaviour
{
    public static AutoLauncher Instance { get; private set; }

    private static bool launchRequested;
    private static string pendingMapFolder = "TEST";
    private static int pendingAiCount = 1;
    private static int pendingAiDifficulty = 2;

    /// <summary>
    /// Assure l'unicité de l'AutoLauncher et le conserve entre les scènes.
    /// </summary>
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

    /// <summary>
    /// S'abonne à l'événement de chargement de scène.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Se désabonne de l'événement de chargement de scène.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Réinitialise l'instance statique si cet AutoLauncher est détruit.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Demande le lancement d'une partie avec les paramètres fournis.
    /// </summary>
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

    /// <summary>
    /// Déclenche la création de la partie lorsque la scène Game est chargée.
    /// </summary>
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

    /// <summary>
    /// Prépare l'attribution aléatoire des structures aux joueurs.
    /// </summary>
    private StructureAttribution PrepareStructureAttribution(string mapFolderName, int aiCount)
    {
        var campAssignment = new StructureAttribution();
        campAssignment.Setup(mapFolderName);

        int totalPlayers = 1 + aiCount;
        campAssignment.AssignRandomOwners(totalPlayers);
        Debug.Log($"[AutoLauncher] Structures attribuees aleatoirement pour {totalPlayers} joueurs.");

        return campAssignment;
    }

    /// <summary>
    /// Journalise les propriétaires des points de départ après attribution.
    /// </summary>
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

    /// <summary>
    /// Recrée le générateur de carte et relance la génération avec les nouvelles données.
    /// </summary>
    private MapGenerator CreateFreshMapGeneratorAndGenerate(string mapFolderName)
    {
        MapGenerator mapGenerator = Object.FindAnyObjectByType<MapGenerator>();

        if (mapGenerator != null)
        {
            Debug.Log("AutoLauncher: Found existing MapGenerator in scene - replacing it to apply new owners.");
            Object.DestroyImmediate(mapGenerator.gameObject);
        }

        ManagerController.initializePermanentGameObject();

        GameObject mapContainer = new GameObject("MAP");
        mapGenerator = mapContainer.AddComponent<MapGenerator>();
        mapGenerator.LoadAndGenerate(mapFolderName);
        
        return mapGenerator;
    }

    /// <summary>
    /// Instancie les IA demandées avec la difficulté spécifiée.
    /// </summary>
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

