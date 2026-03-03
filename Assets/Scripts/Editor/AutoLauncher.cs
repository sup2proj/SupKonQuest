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
                Debug.Log("AutoLauncher : MapGenerator injecté dynamiquement.");
            }
        }
    }
}