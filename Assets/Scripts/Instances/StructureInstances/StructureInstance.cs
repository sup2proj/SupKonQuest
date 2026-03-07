using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

// Ce script doit être rattaché à un GO directement dans la scène 
// et il doit faire référence à l'structureManager, pour pouvoir utiliser la fonction SpawnUnitByTypeAtPosition

public class StructureInstance : MonoBehaviour
{
    private static StructureInstance currentlySelected = null;
    
    [Header("Data")]
    [SerializeField] private StructureData structureData;
    public StructureManager structureManager; //Permet d'appeler le structureManager
    private Vector3 structurePosition; // Récupère la position de la structure dans la scène
    public static float x; // Coordonnée X de la structure
    public static float y; // Coordonnée Y de la structure
    private Queue<UnitData> unitQueue = new Queue<UnitData>(); // File d'attente pour les unités à créer
    private bool isSpawning = false; // Indique si la structure est actuellement en train de créer des unités
    public bool neutralStructure;
    public StructureType structureType;	

    [Header("Units")]
    public List<UnitsType> unitsProtectorTypes = new List<UnitsType>();
    public int UnitsProtector;
    public bool isAlive;

    [Header("Player")]
    public PlayerNumber player;
    private Outline outline;
    private Collider structureCollider;

    void Awake()
    {
        outline = GetComponent<Outline>();
        structureCollider = GetComponent<Collider>();
        
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }
        outline.enabled = false;
        if (structureCollider == null)
        {
            structureCollider = gameObject.AddComponent<BoxCollider>();
        }
    }

    void Start()
    {
        structurePosition = transform.position; // Initialise la position au démarrage
        x = structurePosition.x; // Extrait la coordonnée X
        y = structurePosition.z; // Extrait la coordonnée Z (Y dans le plan 3D Unity)
        UnSelected();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddToQueue(UnitsType.Infantry);
        }

        if (!isSpawning && unitQueue.Count > 0)
        {
            StartCoroutine(SpawnUnitsWithDelay(structurePosition));
        }

        DetectClickOutside();
    }

    public void AddToQueue(UnitsType type)
    {
        UnitData data = structureManager.unitData.Find(d => d.type == type);
        if (data != null)
        {
            unitQueue.Enqueue(data);
        }
    }

    public void OnMouseDown()
    {
        Selected();
		Debug.Log($"Position de la structure: {structurePosition} | X: {x}, Y: {y}");
    }

    void DetectClickOutside()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!outline.enabled) return;
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Vérifie si on a cliqué sur une autre structure
                StructureInstance otherStructure = hit.collider.GetComponent<StructureInstance>();
                // Si c'est une autre structure, on ne fait rien
                // (l'autre structure va se sélectionner via son OnMouseDown)
                if (otherStructure != null && otherStructure != this)
                {
                    // Désélectionne cette structure sans cacher les boutons
                    // (la nouvelle structure va afficher ses propres boutons)
                    UnSelected();
                    return;
                }
                
                // Si le clic n'est pas sur cette structure (ou ses enfants) et pas sur une autre structure
                if (!hit.collider.transform.IsChildOf(transform) && otherStructure == null)
                {
                    UnSelected();
                    ActionInterface.Instance.HideAllButtons();
                }
            }
            else
            {
                // Clic dans le vide → désélection
                UnSelected();
                ActionInterface.Instance.HideAllButtons();
                
            }
        }
    }

    public void Selected()
    {
        Debug.Log($"Structure {name} sélectionnée (Type: {structureType}).");
        
        // Si une autre structure était déjà sélectionnée, on la désélectionne
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.UnSelected();
        }
        
        // Cette structure devient la structure sélectionnée
        currentlySelected = this;
        
        if (outline != null)
            outline.enabled = true;
        else
            Debug.LogWarning($"[StructureInstance] Composant Outline manquant sur {name}.");
        
        ActionInterface.ShowStructureButtons(structureType);
    }

    public void UnSelected()
    {
        Debug.Log($"Structure {name} désélectionnée.");
        
        // Si c'est la structure actuellement sélectionnée, on efface la référence
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }
        
        // Restaure la couleur du bâtiment
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.white;
        outline.enabled = false;
    }

    private IEnumerator SpawnUnitsWithDelay(Vector3 position)
    {
        isSpawning = true;
        while (unitQueue.Count > 0)
        {
            UnitData data = unitQueue.Dequeue();
            structureManager.SpawnUnitByTypeAtPosition(data.type, position);
            yield return new WaitForSeconds(data.creationTime);
        }
        isSpawning = false;
    }
}