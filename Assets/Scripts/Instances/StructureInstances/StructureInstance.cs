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
    private Vector3 structurePosition; // Récupère la position de la structure dans la scène
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
        structurePosition = transform.position;
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
        UnitData data = StructureManager.Instance.unitData.Find(d => d.type == type);
        if (data != null)
        {
            unitQueue.Enqueue(data);
        }
    }

    public void OnMouseDown()
    {
        Selected();
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
        
        // Transmettre les coordonnées de la structure à l'ActionInterface
        ActionInterface.SetSelectedStructure(this, structurePosition);
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
            bool isPoweredUnit = false;
            StructureManager.Instance.SpawnUnitByTypeAtPosition(data.type, position.x, position.z, isPoweredUnit);
            yield return new WaitForSeconds(data.creationTime);
        }
        isSpawning = false;
    }
}