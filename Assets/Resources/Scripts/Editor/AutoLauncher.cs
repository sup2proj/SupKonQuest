using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class AutoLauncher
{
    static AutoLauncher()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            string mapFolderName = "TEST";
            // string mapFolderName = "EUROPE";
            // string mapFolderName = "LOL";
            MapGenerator mapGenerator = Object.FindAnyObjectByType<MapGenerator>();

            if (mapGenerator == null)
            {
                ManagerController.initializePermanentGameObject();
            
                GameObject mapContainer = new GameObject("AUTO_MAP_GENERATOR"); 
                mapGenerator = mapContainer.AddComponent<MapGenerator>();
            
                mapGenerator.LoadAndGenerate(mapFolderName);
                Debug.Log($"AutoLauncher : Monde '{mapFolderName}' généré.");
            }

            Camera mainCam = Camera.main;
            if (mainCam != null) 
            {
                CameraMouvement camMovement = mainCam.GetComponent<CameraMouvement>();
                if (camMovement == null) 
                    camMovement = mainCam.gameObject.AddComponent<CameraMouvement>();

                camMovement.SetUpCamera(mapGenerator.mapWidth, mapGenerator.mapHeight);
            }
            else 
            {
                Debug.LogWarning("AutoLauncher : Impossible de trouver la caméra MainCamera !");
            }
        }
    }
}