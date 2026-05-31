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
    
    /// <summary>
    /// Initialise les composants visuels et physiques de la structure.
    /// </summary>
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
    
    /// <summary>
    /// Définit l'identifiant du joueur propriétaire de la structure.
    /// </summary>
    public void InitializePlayerId(int owner)
    {
        playerId = owner;
    }
    
    /// <summary>
    /// Initialise l'état runtime, l'UI et lance la production de la structure.
    /// </summary>
    void Start()
    {
        structurePosition = transform.position;
        currentHealth = health;
        InitHealthBar();
        UnSelected();
        StartCoroutine(ProcessProductionQueue());
    }

    /// <summary>
    /// Nettoie l'état de sélection lorsque la structure est détruite.
    /// </summary>
    void OnDestroy()
    {
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }
    }

    /// <summary>
    /// Gère les entrées de test, l'orientation de la barre de vie et les clics globaux.
    /// </summary>
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

        if (Instance == this)
        {
            HandleGlobalStructureClick();
        }
    }

    /// <summary>
    /// Gestion centralisee des clics sur les structures
    /// Cette methode n'est executee qu'une fois par frame (par l'Instance principale)
    /// Détecte le clic global sur les structures et sélectionne la plus proche.
    /// </summary>
    private void HandleGlobalStructureClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        RaycastHit[] hits = Physics.RaycastAll(ray);
        StructureInstance closestStructure = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            StructureInstance structure = hit.collider.GetComponent<StructureInstance>();
            if (structure == null)
            {
                structure = hit.collider.GetComponentInParent<StructureInstance>();
            }

            if (structure != null && hit.distance < closestDistance)
            {
                closestStructure = structure;
                closestDistance = hit.distance;
            }
        }

        if (closestStructure != null)
        {
            closestStructure.OnStructureClicked();
        }
        else
        {
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
    /// Traite la sélection d'une structure après un clic.
    /// </summary>
    private void OnStructureClicked()
    {
        int currentPlayerId = PlayerManager.Instance.GetActivePlayerId();

        if (Defeat.IsPlayerDefeated(currentPlayerId))
            return;
        
        if (playerId == currentPlayerId)
            Selected();
    }

    /// <summary>
    /// Configure la barre de vie de la structure.
    /// </summary>
    private void InitHealthBar()
    {
        if (healthBar == null)
            return;

        healthBar.transform.localPosition = (1.1f * Vector3.up);
        healthBar.SetMaxHealth(health);
        healthBar.SetHealth(health);
    }

    /// <summary>
    /// Ajoute une unité standard à la file de production.
    /// </summary>
    public void AddToQueue(UnitsType type)
    {
        AddToQueue(type, false);
    }

    /// <summary>
    /// Ajoute une unité à la file de production en précisant si elle est protectrice.
    /// </summary>
    public void AddToQueue(UnitsType type, bool isProtector)
    {
        if (StructureManager.Instance == null)
            return;

        if (isProtector)
        {
            if (IAInstance.IsAIPlayer(playerId))
            {
                if (unitQueue.Count >= maxQueueSize)
                    return;
            }
        }

        if (unitQueue.Count >= maxQueueSize)
            return;
        
        UnitData data = StructureManager.Instance.unitData.Find(d => d.type == type);
        if (data != null)
        {
            unitQueue.Enqueue(data);
            unitQueueProtectorFlags.Enqueue(isProtector);
        }
    }

    /// <summary>
    /// Traite en continu la file de production des unités de la structure.
    /// </summary>
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
                    bool spawned = false;

                    if (Unity.Netcode.NetworkManager.Singleton != null && 
                        Unity.Netcode.NetworkManager.Singleton.IsClient && 
                        !Unity.Netcode.NetworkManager.Singleton.IsServer)
                    {
                        NetworkSpawner.Instance.RequestSpawnUnitServerRpc(
                            playerId, 
                            data.type, 
                            spawnPosition.x, 
                            spawnPosition.z, 
                            false,
                            isProtector
                        );
                        spawned = true;
                    }
                    else
                    {
                        spawned = StructureManager.Instance.SpawnUnitByTypeAtPosition(
                            playerId,
                            data.type,
                            spawnPosition.x,
                            spawnPosition.z,
                            false,
                            isProtector,
                            this
                        );
                    }

                    Debug.Log($"[StructureInstance] {name} : Spawn queued unit {data.type} (protector={isProtector}) -> {(spawned ? "OK" : "FAILED")}");
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// Marque la structure comme sélectionnée et affiche son interface associée.
    /// </summary>
    public void Selected()
    {
        Debug.Log($"Structure {name} selectionnee (Type: {structureType}).");

        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.UnSelected();
        }

        currentlySelected = this;

        if (outline != null)
            outline.enabled = true;
        else
            Debug.LogWarning($"[StructureInstance] Composant Outline manquant sur {name}.");

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
                    InterfaceInstance.Instance.showUnitsNextToStructure(t, true);
                else
                    InterfaceInstance.Instance.showUnitsNextToStructure(t, false);
        }
    }

    /// <summary>
    /// Retire la sélection de la structure et masque son interface.
    /// </summary>
    public void UnSelected()
    {
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.white;
        outline.enabled = false;
        if (InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.HideStructureInterface();
        }
    }

    /// <summary>
    /// Recherche une structure à partir de son identifiant d'instance Unity.
    /// </summary>
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

    /// <summary>
    /// Retourne les unités alliées situées dans un rayon donné autour de la structure.
    /// </summary>
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
            Vector3 d = unit.transform.position - center;
            if (d.sqrMagnitude <= r2)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    /// <summary>
    /// Retourne les unités alliées présentes dans le rayon configuré de détection.
    /// </summary>
    public List<UnitInstance> GetUnitsWithinConfiguredRadius()
    {
        return GetUnitsWithinRadius(unitsFarRadius);
    }

    
    /// <summary>
    /// Centralise l'application du nom de territoire et adapte la couleur selon le playerId
    /// Applique ou met à jour le nom du territoire affiché par la structure.
    /// </summary>
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

    /// <summary>
    /// Récupère le composant d'affichage du nom de territoire.
    /// </summary>
    private TerritoryStructureName GetTerritoryStructureName()
    {
        if (territoryStructureName == null)
            territoryStructureName = GetComponent<TerritoryStructureName>() ?? GetComponentInChildren<TerritoryStructureName>(true);

        return territoryStructureName;
    }
    
    /// <summary>
    /// Charge des données de carte depuis un chemin de ressources.
    /// </summary>
    public static MapJsonData LoadDataFromPath(string path) {
        TextAsset targetFile = UnityEngine.Resources.Load<TextAsset>(path);
        if (targetFile != null) {
            return JsonUtility.FromJson<MapJsonData>(targetFile.text);
        }
        return null;
    }
}
