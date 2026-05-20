using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public partial class StructureInstance : MonoBehaviour
{
    private static StructureInstance currentlySelected = null;
    public static StructureInstance Instance;

    public static StructureInstance CurrentlySelected => currentlySelected;
    public Vector3 StructurePosition => structurePosition;

    [Header("Data")]
    private Vector3 structurePosition;

    private Queue<UnitData> unitQueue = new Queue<UnitData>();
    private Queue<bool> unitQueueProtectorFlags = new Queue<bool>();
    [Header("Production")]
    [SerializeField, Min(1)] private int maxQueueSize = 5;
    public bool neutralStructure;
    public StructureType structureType;    

    [Header("Units")]
    public List<GameObject> unitsProtectorTypes = new List<GameObject>();

    [Header("Statistics")]
    public int playerId;
    public int health = 1000;
    public int currentHealth;
    private Outline outline;
    private Collider structureCollider;
    public int territoryId;
    public string territoryName;
    
    [Header("UI")]
    [SerializeField] public HealthBar healthBar;
    [SerializeField] public TerritoryStructureName territoryStructureName;

    [Header("Detection")]
    [SerializeField] private float unitsFarRadius = 5f;
    
    void Awake()
    {
        Instance = this;
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
    
    public void InitializePlayerId(int owner)
    {
        playerId = owner;
    }
    
    void Start()
    {
        structurePosition = transform.position;
        currentHealth = health;
        InitHealthBar();
        UnSelected();
        StartCoroutine(ProcessProductionQueue());
    }

    void OnDestroy()
    {
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddToQueue(UnitsType.Infantry);
        }

        if (healthBar != null && healthBar.isActiveAndEnabled && Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            healthBar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        // Gestion centralisee des clics (une seule fois par frame)
        if (Instance == this)
        {
            HandleGlobalStructureClick();
        }
    }

    /// <summary>
    /// Gestion centralisee des clics sur les structures
    /// Cette methode n'est executee qu'une fois par frame (par l'Instance principale)
    /// </summary>
    private void HandleGlobalStructureClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        // Ignorer les clics sur l'UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        // Utiliser RaycastAll pour trouver TOUS les colliders, y compris les Triggers
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        Debug.Log($"[StructureClick] Raycasting detecte {hits.Length} colliders");

        StructureInstance closestStructure = null;
        float closestDistance = float.MaxValue;

        // Parcourir tous les hits et trouver la structure la plus proche
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Debug.Log($"[StructureClick] Hit {i}: {hit.collider.gameObject.name} a distance {hit.distance}");

            // Chercher une StructureInstance sur ce collider ou ses parents
            StructureInstance structure = hit.collider.GetComponent<StructureInstance>();
            if (structure == null)
            {
                structure = hit.collider.GetComponentInParent<StructureInstance>();
            }

            // Garder la structure la plus proche
            if (structure != null && hit.distance < closestDistance)
            {
                Debug.Log($"[StructureClick] Structure trouvee: {structure.name} a distance {hit.distance}");
                closestStructure = structure;
                closestDistance = hit.distance;
            }
        }

        if (closestStructure != null)
        {
            Debug.Log($"[StructureClick] Selection: {closestStructure.name}");
            closestStructure.OnStructureClicked();
        }
        else
        {
            // Le clic n'a touche aucune structure - deselectionner si une structure est selectionnee
            Debug.Log($"[StructureClick] Aucune structure trouvee");
            if (currentlySelected != null)
            {
                currentlySelected.UnSelected();
                if (ActionInterface.Instance != null)
                    ActionInterface.Instance.HideAllButtons();
            }
        }
    }

    /// <summary>
    /// Appele quand cette structure est cliquee
    /// </summary>
    private void OnStructureClicked()
    {
        Debug.Log($"[{name}] Structure cliquee (PlayerId: {playerId})");
        int currentPlayerId = PlayerManager.Instance.GetActivePlayerId();
        Debug.Log($"[{name}] PlayerActif: {currentPlayerId}");

        if (Defeat.IsPlayerDefeated(currentPlayerId))
        {
            Debug.Log($"[{name}] Selection refusee: le joueur {currentPlayerId} est elimine.");
            return;
        }
        
        if (playerId == currentPlayerId)
        {
            Debug.Log($"[{name}] Selection accordee!");
            Selected();
        }
        else
        {
            Debug.Log($"[{name}] Selection refusee (PlayerId: {playerId} != {currentPlayerId})");
        }
    }

    private void InitHealthBar()
    {
        if (healthBar == null)
        {
            Debug.LogWarning($"[StructureInstance] {name} : healthBar non assignee dans l'inspector.", this);
            return;
        }

        healthBar.transform.localPosition = (1.1f * Vector3.up);
        healthBar.SetMaxHealth(health);
        healthBar.SetHealth(health);
    }

    public void AddToQueue(UnitsType type)
    {
        AddToQueue(type, false);
    }

    public void AddToQueue(UnitsType type, bool isProtector)
    {
        if (StructureManager.Instance == null)
            return;

        // If this enqueue is for a protector and the structure belongs to the IA player,
        // enforce maxQueueSize to avoid infinite protector spawns.
        if (isProtector)
        {
            if (IAInstance.IsAIPlayer(playerId))
            {
                if (unitQueue.Count >= maxQueueSize)
                {
                    Debug.LogWarning($"[StructureInstance] {name} cannot enqueue protector {type}: protector-queue full ({unitQueue.Count}/{maxQueueSize}).");
                    return;
                }
            }
        }

        if (unitQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning($"[StructureInstance] {name} cannot enqueue {type}: queue full ({unitQueue.Count}/{maxQueueSize}).");
            return;
        }
        
        UnitData data = StructureManager.Instance.unitData.Find(d => d.type == type);
        if (data != null)
        {
            unitQueue.Enqueue(data);
            unitQueueProtectorFlags.Enqueue(isProtector);
            Debug.Log($"[StructureInstance] {name} : Enqueued unit {type} (isProtector={isProtector}). QueueSize={unitQueue.Count}");
        }
    }

    private IEnumerator ProcessProductionQueue()
    {
        while (true)
        {
            if (unitQueue.Count > 0)
            {
                UnitData data = unitQueue.Dequeue();
                bool isProtector = false;
                if (unitQueueProtectorFlags.Count > 0)
                    isProtector = unitQueueProtectorFlags.Dequeue();
                Debug.Log($"[StructureInstance] {name} : Dequeued unit {data?.type.ToString() ?? "null"} (isProtector={isProtector}). RemainingQueue={unitQueue.Count}");
                if (data == null)
                {
                    yield return null;
                    continue;
                }

                yield return new WaitForSeconds(Mathf.Max(0f, data.creationTime));

                if (StructureManager.Instance != null)
                {
                    Vector3 spawnPosition = transform.position;
                    bool spawned = StructureManager.Instance.SpawnUnitByTypeAtPosition(
                        playerId,
                        data.type,
                        spawnPosition.x,
                        spawnPosition.z,
                        false,
                        isProtector,
                        this
                    );

                    Debug.Log($"[StructureInstance] {name} : Spawn queued unit {data.type} (protector={isProtector}) -> {(spawned ? "OK" : "FAILED")}");
                }
            }

            yield return null;
        }
    }

    public void Selected()
    {
        Debug.Log($"Structure {name} selectionnee (Type: {structureType}).");

        // Si une autre structure etait deja selectionnee, on la deselectionne
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.UnSelected();
        }

        // Cette structure devient la structure selectionnee
        currentlySelected = this;

        if (outline != null)
            outline.enabled = true;
        else
            Debug.LogWarning($"[StructureInstance] Composant Outline manquant sur {name}.");

        // Transmettre les coordonnees de la structure a l'ActionInterface
        ActionInterface.SetSelectedStructure(this, structurePosition);
        ActionInterface.ShowStructureButtons(structureType);

        if (InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.showInterfaceForStructure();
            var nearbyUnits = GetUnitsWithinConfiguredRadius();
            var uniqueTypes = new HashSet<UnitsType>();
            foreach (var unit in nearbyUnits)
            {
                if (unit == null) continue;
                if (unit.unitData == null) continue;
                uniqueTypes.Add(unit.unitData.type);
            }

            foreach (var t in uniqueTypes)
                if (structureType == StructureType.NeutralStructure)
                {
                    InterfaceInstance.Instance.showUnitsNextToStructure(t, true);
                }
                else
                {
                    InterfaceInstance.Instance.showUnitsNextToStructure(t, false);
                }
        }
    }

    public void UnSelected()
    {
        Debug.Log($"Structure {name} deselectionnee.");

        // Si c'est la structure actuellement selectionnee, on efface la reference
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }

        // Restaure la couleur du batiment
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.white;
        outline.enabled = false;
        if (InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.HideStructureInterface();
        }
    }

    public static StructureInstance FindByInstanceId(int instanceId)
    {
        if (instanceId == -1)
            return null;

        StructureInstance[] structures = Object.FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
        for (int i = 0; i < structures.Length; i++)
        {
            StructureInstance structure = structures[i];
            if (structure != null && structure.GetInstanceID() == instanceId)
                return structure;
        }

        return null;
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
            if (unit.playerId != playerId) continue;
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

    // Centralise l'application du nom de territoire et adapte la couleur selon le playerId
    public void ApplyTerritoryName(string territory)
    {
        TerritoryStructureName t = GetTerritoryStructureName();

        if (t != null)
        {
            if (!string.IsNullOrWhiteSpace(territory))
            {
                territoryName = territory;
                t.SetTerritoryName(territory);
            }

            t.SetColor(PlayerManager.GetPlayerColor(playerId));
        }
    }

    private TerritoryStructureName GetTerritoryStructureName()
    {
        if (territoryStructureName == null)
            territoryStructureName = GetComponent<TerritoryStructureName>() ?? GetComponentInChildren<TerritoryStructureName>(true);

        return territoryStructureName;
    }
    
    public static MapJsonData LoadDataFromPath(string path) {
        TextAsset targetFile = UnityEngine.Resources.Load<TextAsset>(path);
        if (targetFile != null) {
            return JsonUtility.FromJson<MapJsonData>(targetFile.text);
        }
        return null;
    }
}
