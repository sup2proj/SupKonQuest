using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class StructureInstance : MonoBehaviour
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

        // Gestion centralisée des clics (une seule fois par frame)
        if (Instance == this)
        {
            HandleGlobalStructureClick();
        }
    }

    /// <summary>
    /// Gestion centralisée des clics sur les structures
    /// Cette méthode n'est exécutée qu'une fois par frame (par l'Instance principale)
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
        
        Debug.Log($"[StructureClick] Raycasting détecté {hits.Length} colliders");

        StructureInstance closestStructure = null;
        float closestDistance = float.MaxValue;

        // Parcourir tous les hits et trouver la structure la plus proche
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Debug.Log($"[StructureClick] Hit {i}: {hit.collider.gameObject.name} à distance {hit.distance}");

            // Chercher une StructureInstance sur ce collider ou ses parents
            StructureInstance structure = hit.collider.GetComponent<StructureInstance>();
            if (structure == null)
            {
                structure = hit.collider.GetComponentInParent<StructureInstance>();
            }

            // Garder la structure la plus proche
            if (structure != null && hit.distance < closestDistance)
            {
                Debug.Log($"[StructureClick] Structure trouvée: {structure.name} à distance {hit.distance}");
                closestStructure = structure;
                closestDistance = hit.distance;
            }
        }

        if (closestStructure != null)
        {
            Debug.Log($"[StructureClick] Sélection: {closestStructure.name}");
            closestStructure.OnStructureClicked();
        }
        else
        {
            // Le clic n'a touché aucune structure - désélectionner si une structure est sélectionnée
            Debug.Log($"[StructureClick] Aucune structure trouvée");
            if (currentlySelected != null)
            {
                currentlySelected.UnSelected();
                if (ActionInterface.Instance != null)
                    ActionInterface.Instance.HideAllButtons();
            }
        }
    }

    /// <summary>
    /// Appelé quand cette structure est cliquée
    /// </summary>
    private void OnStructureClicked()
    {
        Debug.Log($"[{name}] Structure cliquée (PlayerId: {playerId})");
        int currentPlayerId = PlayerManager.Instance.GetActivePlayerId();
        Debug.Log($"[{name}] PlayerActif: {currentPlayerId}");
        
        if (playerId == currentPlayerId)
        {
            Debug.Log($"[{name}] ✓ Sélection accordée!");
            Selected();
        }
        else
        {
            Debug.Log($"[{name}] ✗ Sélection refusée (PlayerId: {playerId} != {currentPlayerId})");
        }
    }

    private void InitHealthBar()
    {
        if (healthBar == null)
        {
            Debug.LogWarning($"[StructureInstance] {name} : healthBar non assignée dans l'inspector.", this);
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
            var ia = FindFirstObjectByType<IAInstance>();
            int iaPlayerId = ia != null ? ia.PlayerId : -1;
            if (iaPlayerId == playerId)
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

    public void AddProtectorUnit(UnitInstance protectorUnit)
    {
        if (protectorUnit == null)
            return;

        CleanupProtectorUnits();
        GameObject protectorObject = protectorUnit.gameObject;
        if (!unitsProtectorTypes.Contains(protectorObject))
            unitsProtectorTypes.Add(protectorObject);
    }

    public void RemoveProtectorUnit(UnitInstance protectorUnit)
    {
        if (protectorUnit == null)
            return;

        unitsProtectorTypes.Remove(protectorUnit.gameObject);
    }

    private void CleanupProtectorUnits()
    {
        unitsProtectorTypes.RemoveAll(unitObject => unitObject == null);
    }

    public static StructureInstance FindByInstanceId(int instanceId)
    {
        if (instanceId == -1)
            return null;

        StructureInstance[] structures = Object.FindObjectsOfType<StructureInstance>();
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
        if (string.IsNullOrWhiteSpace(territory))
            return;

        TerritoryStructureName t = territoryStructureName;
        if (t == null)
            t = GetComponent<TerritoryStructureName>() ?? GetComponentInChildren<TerritoryStructureName>(true);

        if (t != null)
        {
            t.SetTerritoryName(territory);
            Color c = PlayerManager.GetPlayerColor(playerId);
            t.SetColor(c);
        }
    }
    
    public static MapJsonData LoadDataFromPath(string path) {
        TextAsset targetFile = UnityEngine.Resources.Load<TextAsset>(path);
        if (targetFile != null) {
            return JsonUtility.FromJson<MapJsonData>(targetFile.text);
        }
        return null;
    }
    
   public void TakeDamage(float amount, UnitInstance attacker)
   {
       currentHealth -= Mathf.RoundToInt(amount);
       currentHealth = Mathf.Clamp(currentHealth, 0, health);
   
       if (healthBar != null)
           healthBar.SetHealth(currentHealth);

       // Déclenchement IA uniquement si le comportement IA niveau 2 est actif
       IAInstance ia = FindFirstObjectByType<IAInstance>();
       if (ia != null && ia.DifficultyIA == 2 && attacker != null && attacker.playerId != playerId)
       {
           // On regarde dans le rayon de la structure: s'il y a au moins une unité de combat alliée,
           // on autorise la création de protecteurs.
           bool hasCombatAllyNearby = false;
           var nearbyUnits = GetUnitsWithinConfiguredRadius();
           for (int i = 0; i < nearbyUnits.Count; i++)
           {
               UnitInstance unit = nearbyUnits[i];
               if (unit == null || unit.unitData == null)
                   continue;

               if (unit.playerId != playerId)
                   continue;

               if (unit.unitData is UnitCombatData)
               {
                   hasCombatAllyNearby = true;
                   break;
               }
           }

           if (hasCombatAllyNearby)
           {
               NormalDefense normalDefense = GetComponent<NormalDefense>();
               if (normalDefense == null)
                   normalDefense = gameObject.AddComponent<NormalDefense>();

               normalDefense.MyStructureAttacked(attacker);
           }
       }

       TryTriggerProtectorRetaliation(attacker);
   
       // if (currentHealth <= 0)
       //     Die();
   }

   private void TryTriggerProtectorRetaliation(UnitInstance attacker)
   {
       if (attacker == null || attacker.transform == null)
           return;
       if (attacker.playerId == playerId)
           return;

       CleanupProtectorUnits();

       for (int i = unitsProtectorTypes.Count - 1; i >= 0; i--)
       {
           GameObject protectorObject = unitsProtectorTypes[i];
           if (protectorObject == null)
           {
               unitsProtectorTypes.RemoveAt(i);
               continue;
           }

           UnitInstance protector = protectorObject.GetComponent<UnitInstance>();
           if (protector == null || protector.unitData == null || !protector.unitData.isProtector)
           {
               unitsProtectorTypes.RemoveAt(i);
               continue;
           }

           if (protector.playerId != playerId)
               continue;

           UnitsAnimation protectorAnimation = protectorObject.GetComponent<UnitsAnimation>();
           if (protectorAnimation == null)
               continue;

           float stopDistance = 0.1f;
           if (protector.unitData is UnitCombatData combatData)
               stopDistance = Mathf.Max(0f, combatData.attackRange);

           protectorAnimation.EngageTarget(attacker.transform, stopDistance);
       }
   }
}
