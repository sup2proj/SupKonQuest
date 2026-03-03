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
        // GameObject permanentOverlay = Resources.Load<GameObject>("Sprites/UI/StructureInterface/Interface");
        // if (permanentOverlay != null)
        // {
        //     Instantiate(permanentOverlay);
        // }
        // else
        // {
        //     Debug.LogError("Le prefab Interface n'a pas pu être chargé depuis Resources!");
        // }
    }
}
