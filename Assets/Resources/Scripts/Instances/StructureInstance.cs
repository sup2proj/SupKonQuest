using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class StructureInstance : MonoBehaviour
{
    private static StructureInstance currentlySelected = null;

    public static StructureInstance CurrentlySelected => currentlySelected;
    public Vector3 StructurePosition => structurePosition;

    [Header("Data")]
    [SerializeField] private StructureData structureData;
    private Vector3 structurePosition;

    private Queue<UnitData> unitQueue = new Queue<UnitData>();
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

    [Header("Detection")]
    [SerializeField] private float unitsFarRadius = 5f;
    
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

        if (InterfaceInstance.Instance != null)
        {
			InterfaceInstance.Instance.showInterfaceForStructure();

            // Scan des unités à proximité et affichage des icônes "protectors"
            var nearbyUnits = GetUnitsWithinConfiguredRadius();
            var uniqueTypes = new HashSet<UnitsType>();
            foreach (var unit in nearbyUnits)
            {
                if (unit == null) continue;
                if (unit.unitData == null) continue;
                uniqueTypes.Add(unit.unitData.type);
            }

            foreach (var t in uniqueTypes)
                InterfaceInstance.Instance.showUnitsNextToStructure(t);
        }
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
        if (InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.HideStructureInterface();
        }
    }

    public List<UnitInstance> GetUnitsWithinRadius(float radius)
    {
        var result = new List<UnitInstance>();
        if (radius < 0f) return result;

        float r2 = radius * radius;
        Vector3 center = transform.position;

        foreach (var unit in UnitsRegistry.GetSnapshot())
        {
            if (unit == null) continue;
            Debug.Log($"Checking unit {unit.name} at position {unit.transform.position} against structure {name} at position {center} with radius {radius}.");
            Vector3 d = unit.transform.position - center;
            if (d.sqrMagnitude <= r2)
            {
                result.Add(unit);
                Debug.Log($"Unit {unit.name} is within radius {radius} of structure {name}.");
            }
        }

        return result;
    }

    public List<UnitInstance> GetUnitsWithinConfiguredRadius()
    {
        return GetUnitsWithinRadius(unitsFarRadius);
    }
    
    public static MapJsonData LoadDataFromPath(string path) {
        TextAsset targetFile = Resources.Load<TextAsset>(path);
        if (targetFile != null) {
            return JsonUtility.FromJson<MapJsonData>(targetFile.text);
        }
        return null;
    }
}