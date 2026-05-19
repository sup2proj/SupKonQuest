using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StructureManager : MonoBehaviour
{
    public static StructureManager Instance;
    public List<UnitData> unitData;

    [Header("Sessions / Economy")]
    [SerializeField] private PlayerManager playerManager;

    [Header("Unit Prefabs")]
    [SerializeField] private List<UnitPrefabMapping> unitPrefabMappings = new List<UnitPrefabMapping>();
    private Dictionary<UnitsType, GameObject> unitPrefabDict = new Dictionary<UnitsType, GameObject>();
    private Dictionary<int, List<StructureInstance>> structuresByTerritory = new Dictionary<int, List<StructureInstance>>();

    [Header("Powered units")]
    [SerializeField] private float poweredStatsMultiplier = 1.20f;

    [System.Serializable]
    public class UnitPrefabMapping
    {
        public UnitsType unitType;
        public GameObject prefab;
    }

    void Awake()
    {
        Instance = this;
        foreach (var mapping in unitPrefabMappings)
        {
            unitPrefabDict.Add(mapping.unitType, mapping.prefab);
        }
    }

    // Enregistre une structure dans l'index interne et lui assigne les métadonnées de territoire
    public void RegisterStructure(StructureInstance si, int territoryId, string territoryName)
    {
        if (si == null) return;

        si.territoryId = territoryId;
        si.territoryName = territoryName;

        if (territoryId <= 0) return;

        if (!structuresByTerritory.TryGetValue(territoryId, out var list))
        {
            list = new List<StructureInstance>();
            structuresByTerritory[territoryId] = list;
        }

        if (!list.Contains(si))
            list.Add(si);
    }

    public bool SpawnUnitByTypeAtPosition(int playerId, UnitsType type, float x, float z, bool isPoweredUnit, bool isProtector, StructureInstance sourceStructure = null)
    {
        UnitData data = unitData.Find(d => d.type == type);
        if (data == null)
        {
            return false;
        }
        if (!unitPrefabDict.TryGetValue(type, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"[StructureManager] Aucun prefab configuré dans l'inspecteur pour {type}.");
            return false;
        }
        UnitData runtimeData = Instantiate(data);
        runtimeData.playerId = playerId;
        if (isPoweredUnit)
        {
            runtimeData.isPoweredUnit = true;
            float m = poweredStatsMultiplier;
            runtimeData.maxHealth *= m;
            runtimeData.speed *= m;
            runtimeData.creationTime *= m;
            runtimeData.price = Mathf.RoundToInt(runtimeData.price * m);
            if (type != UnitsType.Support && type != UnitsType.Healer)
            {
                if (runtimeData is UnitCombatData combatData)
                {
                    combatData.attack *= m;
                    combatData.attackSpeed *= m;
                }
            }
        }

        if (isProtector)
        {
            runtimeData.isProtector = true;
        }

        Vector3 position;
        position = new Vector3(x, 0, z - 3);

        GameObject unitGO = Instantiate(prefab, position, Quaternion.identity);

        if (isPoweredUnit)
            ApplyColorTint(unitGO, new Color(1f, 0.35f, 0.35f, 1f));

        if (isProtector)
            ApplyColorTint(unitGO, new Color(0.35f, 1f, 0.35f, 1f));

        UnitInstance instance = unitGO.GetComponent<UnitInstance>();
        if (instance == null)
        {
            Debug.LogWarning($"[StructureManager] Le prefab {prefab.name} ne contient pas de UnitInstance component. Tentative d'ajouter dynamiquement.");
            instance = unitGO.AddComponent<UnitInstance>();
            // Si UnitInstance attend des données à l'Awake/Start, c'est risqué, on logue.
        }

        instance.Initialize(runtimeData);
        BoatTransport.GetOrAdd(instance);
        
        // S'assurer que le composant MovementManager est présent sur l'unité
        if (unitGO.GetComponent<MovementManager>() == null)
        {
            unitGO.AddComponent<MovementManager>();
            Debug.Log($"[StructureManager] MovementManager ajouté dynamiquement à {unitGO.name}");
        }
        
        // Ajout automatique du système de déplacement IA pour les joueurs IA
        // Ajouter MovementEasyNormal uniquement pour les unités IA normales (pas les protecteurs)
        if (IAInstance.IsAIPlayer(playerId) && !isProtector)
        {
            if (unitGO.GetComponent<MovementEasyNormal>() == null)
            {
                unitGO.AddComponent<MovementEasyNormal>();
            }
        }

        if (isProtector && sourceStructure != null)
            sourceStructure.AddProtectorUnit(instance);

        if (playerManager != null)
        {
            var session = playerManager.GetSession(playerId);
            if (session != null)
            {
                session.AddUnit(1);
                StatisticsInterface.Instance.Refresh();
            }
        }
        return true;
    }

    public bool TryGetUnitPrefab(UnitsType type, out GameObject prefab)
    {
        return unitPrefabDict.TryGetValue(type, out prefab) && prefab != null;
    }

    private void ApplyColorTint(GameObject unitGO, Color tint)
    {
        var excludedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Circle","HealthBar"
        };
        var renderers = unitGO.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (excludedNames.Contains(r.gameObject.name))
                continue;
            var mat = r.material;
            if (mat.HasProperty("_Color"))
            {
                mat.color = tint;
            }
        }
    }
}
