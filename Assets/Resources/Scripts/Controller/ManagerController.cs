using UnityEngine;

public class ManagerController : MonoBehaviour
{
    /// <summary>
    /// Initialise les GameObjects permanents du jeu (overlay UI et managers).
    /// </summary>
    public static void initializePermanentGameObject()
    {
        initializePermanentOverlay();
        intializePermanentManager();
    }

    /// <summary>
    /// Charge et instancie le prefab de l'interface de jeu depuis Resources et assure l'EventSystem.
    /// </summary>
    public static void initializePermanentOverlay()
    {
        GameObject permanentOverlay = UnityEngine.Resources.Load<GameObject>("Prefabs/Ui/GameInterface");
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
    
    /// <summary>
    /// Charge et instancie le prefab contenant les objets managers et assure l'EventSystem.
    /// </summary>
    public static void intializePermanentManager()
    {
        GameObject managers = UnityEngine.Resources.Load<GameObject>("Prefabs/Managers");
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
    
    /// <summary>
    /// Crée un EventSystem si aucun n'est présent dans la scène (nécessaire pour l'UI).
    /// </summary>
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

