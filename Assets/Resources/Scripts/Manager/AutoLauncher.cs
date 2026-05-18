using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoLauncher : MonoBehaviour
{
    public static AutoLauncher Instance { get; private set; }

    private static bool launchRequested;
    private static string pendingMapFolder = "TEST";

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

    public static void Request(string folder)
    {
        pendingMapFolder = string.IsNullOrWhiteSpace(folder) ? "TEST" : folder;
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
        CreateGame(pendingMapFolder);
    }

    /// <summary>
    /// Crée et lance la partie : génère la map, configure les camps et la caméra.
    /// </summary>
    public void CreateGame(string mapFolderName = "TEST")
    {
        Debug.Log($"AutoLauncher.CreateGame called with mapFolderName='{mapFolderName}'");
        string localPlayerName = "toto";
        string[] playerList = { "toto" };

        MapGenerator mapGenerator = Object.FindAnyObjectByType<MapGenerator>();

        if (mapGenerator == null)
        {
            ManagerController.initializePermanentGameObject();

            GameObject mapContainer = new GameObject("AUTO_MAP_GENERATOR");
            mapGenerator = mapContainer.AddComponent<MapGenerator>();
            mapGenerator.LoadAndGenerate(mapFolderName);
            Debug.Log($"AutoLauncher : Monde '{mapFolderName}' généré.");
        }
        else
        {
            Debug.Log("AutoLauncher: Found existing MapGenerator in scene, skipping creation.");
        }

        StructureAttribution campAssignment = new StructureAttribution();
        campAssignment.Setup(mapFolderName);
        campAssignment.SetCampAssignment(playerList);
        (int startCameraPositionX, int startCameraPositionY) = campAssignment.GetPlayerCameraStartPosition(localPlayerName);
        Debug.Log($"1-{campAssignment.campAssignments[0].playerName}, 2-{campAssignment.campAssignments[1].playerName}");
        Debug.Log($"1-{campAssignment.campAssignments[0].playerType}, 2-{campAssignment.campAssignments[1].playerType}");

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = Object.FindObjectOfType<Camera>();
        }

        if (mainCam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        CameraMouvement camMovement = mainCam.GetComponent<CameraMouvement>();
        if (camMovement == null)
            camMovement = mainCam.gameObject.AddComponent<CameraMouvement>();

        camMovement.SetUpCamera(mapGenerator.mapWidth, mapGenerator.mapHeight, startCameraPositionX, startCameraPositionY);
    }
}
