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
            string localPlayerName ="toto";
            string[] playerList = { "toto" };
            
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

            StructureAttribution campAssignment = new StructureAttribution();
            campAssignment.Setup(mapFolderName);
            campAssignment.SetCampAssignment(playerList);
            (int startCameraPositionX,int startCameraPositionY)=campAssignment.GetPlayerCameraStartPosition(localPlayerName);
            Debug.Log($"1-{campAssignment.campAssignments[0].playerName}, 2-{campAssignment.campAssignments[1].playerName}");
            Debug.Log($"1-{campAssignment.campAssignments[0].playerType}, 2-{campAssignment.campAssignments[1].playerType}");

            Camera mainCam = Camera.main;
            if (mainCam != null) 
            {
                CameraMouvement camMovement = mainCam.GetComponent<CameraMouvement>();
                if (camMovement == null) 
                    camMovement = mainCam.gameObject.AddComponent<CameraMouvement>();

                camMovement.SetUpCamera(mapGenerator.mapWidth, mapGenerator.mapHeight, startCameraPositionX, startCameraPositionY);
            }
            else 
            {
                Debug.LogWarning("AutoLauncher : Impossible de trouver la caméra MainCamera !");
            }
        }
    }
}