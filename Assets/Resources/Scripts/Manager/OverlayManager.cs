using UnityEngine;

public class OverlayManager : MonoBehaviour
{
    void Start()
    {
        initializePermanentOverlay();
    }

    void Update()
    {
        
    }

    public static void initializePermanentOverlay()
    {
        GameObject permanentOverlay = Resources.Load<GameObject>("Prefabs/Ui/GameInterface");
        initializeEventSystem();
        if (permanentOverlay != null)
        {
            Instantiate(permanentOverlay);
        }
        else
        {
            Debug.LogError("Le prefab Interface n'a pas pu être chargé depuis Resources!");
        }
    }
    
    public static void initializeEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}

