using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
            if (Object.FindAnyObjectByType<MapGenerator>() == null)
            {
                
                OverlayManager.initializePermanentOverlay();
                
                GameObject map = new GameObject("AUTO_MAP_GENERATOR"); 
                
                GameObject structureManagerPrefab = Resources.Load<GameObject>("Prefabs/Manager/StructureManager");

                if (structureManagerPrefab != null)
                {
                    GameObject structureManagerInstance = Object.Instantiate(structureManagerPrefab);
                    structureManagerInstance.name = "StructureManager";
                }
                else
                {
                    Debug.LogWarning("AutoLauncher : Prefab StructureManager introuvable !");
                }

                
                MapGenerator mapGenerator = map.AddComponent<MapGenerator>();
                mapGenerator.LoadAndGenerate("TEST");
                Debug.Log("AutoLauncher : MapGenerator injecté dynamiquement.");
                // Camera mainCamera = Camera.main;
                // mainCamera.AddComponent<cameraMouvement>();
                // Camera.main.AddComponent<cameraMouvement>();
            }
            Camera mainCam = Camera.main;
            if (mainCam != null) 
            {
                mainCam.gameObject.AddComponent<cameraMouvement>();
            }
            else 
            {
                Debug.LogWarning("AutoLauncher : Impossible de trouver la caméra MainCamera !");
            }
        }
    }
}