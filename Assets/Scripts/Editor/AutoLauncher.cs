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
                GameObject bootstrapper = new GameObject("AUTO_MAP_GENERATOR");
                bootstrapper.AddComponent<MapGenerator>();
                
                OverlayManager.initializePermanentOverlay();
                
                GameObject gameObject = new GameObject("AUTO_MAP_GENERATOR");
                MapGenerator mapGenerator = gameObject.AddComponent<MapGenerator>();
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