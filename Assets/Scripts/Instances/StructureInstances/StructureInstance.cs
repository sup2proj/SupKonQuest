using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

// Ce script doit être rattaché à un GO directement dans la scène 
// et il doit faire référence à l'unitsManager, pour pouvoir utiliser la fonction SpawnUnitByTypeAtPosition

public class StructureInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StructureData structureData;
    public UnitsManager unitsManager; //Permet d'appeler l'unitsManager
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
        structurePosition = transform.position; // Initialise la position au démarrage
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
        UnitData data = unitsManager.unitData.Find(d => d.type == type);
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

        // Vérifie uniquement si la structure est sélectionnée
        if (!outline.enabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Si le clic n'est pas sur cette structure (ou ses enfants)
                if (!hit.collider.transform.IsChildOf(transform))
                {
                    UnSelected();
                    ActionInterface.Instance.HideStructureButtons();
                }
            }
            else
            {
                // Clic dans le vide → désélection
                UnSelected();
                ActionInterface.Instance.HideStructureButtons();
                
            }
        }
    }

    public void Selected()
    {
        Debug.Log($"Structure {name} sélectionnée.");
        if (outline != null)
            outline.enabled = true;
        else
            Debug.LogWarning($"[StructureInstance] Composant Outline manquant sur {name}.");
        
        ActionInterface.ShowStructureButtons();
    }

    public void UnSelected()
    {
        Debug.Log($"Structure {name} désélectionnée.");
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
            unitsManager.SpawnUnitByTypeAtPosition(data.type, position);
            yield return new WaitForSeconds(data.creationTime);
        }
        isSpawning = false;
    }
}