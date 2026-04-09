using UnityEngine;

public class ManagerController : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public static void initializePermanentGameObject()
    {
        initializePermanentOverlay();
        intializePermanentManager();
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
    
    public static void intializePermanentManager()
    {
        GameObject managers = Resources.Load<GameObject>("Prefabs/Managers");
        initializeEventSystem();
        if (managers != null)
        {
            Instantiate(managers);
        }
        else
        {
            Debug.LogError("Le prefab des managers n'a pas pu être chargé depuis Resources!");
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

