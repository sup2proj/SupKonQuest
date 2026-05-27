using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StructureManager : MonoBehaviour
{
    public static StructureManager Instance;
    private const float DefaultSpawnZOffset = -3f;
    private const int HarbourWaterSearchRadiusInTiles = 8;
    public List<UnitData> unitData;

    [Header("Sessions / Economy")]
    [SerializeField] private PlayerManager playerManager;

    [Header("Unit Prefabs")]
    [SerializeField] private List<UnitPrefabMapping> unitPrefabMappings = new List<UnitPrefabMapping>();
    private Dictionary<UnitsType, GameObject> unitPrefabDict = new Dictionary<UnitsType, GameObject>();
    private Dictionary<int, List<StructureInstance>> structuresByTerritory = new Dictionary<int, List<StructureInstance>>();

    [Header("Powered units")]
    [SerializeField] private float poweredStatsMultiplier = 1.20f;
    [SerializeField] private float protectorHealthMultiplier = 2f;

    [System.Serializable]
    public class UnitPrefabMapping
    {
        public UnitsType unitType;
        public GameObject prefab;
    }

    /// <summary>
    /// Construit l'index interne des préfabs d'unités par type.
    /// </summary>
    void Awake()
    {
        Instance = this;
        foreach (var mapping in unitPrefabMappings)
        {
            unitPrefabDict.Add(mapping.unitType, mapping.prefab);
        }
    }

    // Enregistre une structure dans l'index interne et lui assigne les métadonnées de territoire
    /// <summary>
    /// Enregistre une structure dans l'index des structures d'un territoire.
    /// </summary>
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

    /// <summary>
    /// Instancie une unité à partir de son type à une position donnée.
    /// </summary>
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
            runtimeData.maxHealth *= protectorHealthMultiplier;
        }

        if (sourceStructure != null && sourceStructure.structureType == StructureType.Harbour && !IsBoatType(type))
        {
            Debug.LogWarning($"[StructureManager] Spawn refuse depuis un Harbour pour {type}: seuls les bateaux peuvent apparaitre depuis un port.");
            return false;
        }

        Vector3 position = ResolveSpawnPosition(type, x, z, sourceStructure);

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
        if (isProtector && sourceStructure != null)
            instance.SetProtectorSourceStructure(sourceStructure);
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

    /// <summary>
    /// Récupère le prefab associé à un type d'unité.
    /// </summary>
    public bool TryGetUnitPrefab(UnitsType type, out GameObject prefab)
    {
        return unitPrefabDict.TryGetValue(type, out prefab) && prefab != null;
    }

    /// <summary>
    /// Applique une teinte colorée aux rendus de l'unité instanciée.
    /// </summary>
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

    /// <summary>
    /// Détermine la position d'apparition la plus appropriée pour une unité.
    /// </summary>
    private Vector3 ResolveSpawnPosition(UnitsType type, float x, float z, StructureInstance sourceStructure)
    {
        Vector3 fallback = new Vector3(x, 0f, z + DefaultSpawnZOffset);

        if (sourceStructure == null || sourceStructure.structureType != StructureType.Harbour || !IsBoatType(type))
            return fallback;

        if (TryFindNearestWaterSpawn(sourceStructure.transform.position, fallback, out Vector3 waterPosition))
            return waterPosition;

        Debug.LogWarning($"[StructureManager] Aucune tuile d'eau proche trouvee pour {type}; fallback sur {fallback}.");
        return fallback;
    }

    /// <summary>
    /// Recherche une position d'apparition d'eau proche d'un port.
    /// </summary>
    private bool TryFindNearestWaterSpawn(Vector3 harbourPosition, Vector3 preferredPosition, out Vector3 spawnPosition)
    {
        spawnPosition = preferredPosition;

        MapGenerator map = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();
        if (map == null || map.allTiles == null || map.tileSize <= 0f)
            return false;

        if (map.TryGetTileAtWorldPosition(preferredPosition, out TileData preferredTile) && TileData.IsNavigableWater(preferredTile))
        {
            spawnPosition = TileData.ToWorldPosition(map, preferredTile);
            return true;
        }

        float maxDistance = Mathf.Max(map.tileSize, HarbourWaterSearchRadiusInTiles * map.tileSize);
        float maxDistanceSq = maxDistance * maxDistance;
        float bestPreferredDistanceSq = float.MaxValue;
        float bestHarbourDistanceSq = float.MaxValue;
        bool found = false;

        int width = map.allTiles.GetLength(0);
        int height = map.allTiles.GetLength(1);

            for (int tileX = 0; tileX < width; tileX++)
            {
                for (int tileY = 0; tileY < height; tileY++)
                {
                    TileData tile = map.allTiles[tileX, tileY];
                    if (!TileData.IsNavigableWater(tile))
                        continue;

                    Vector3 candidate = TileData.ToWorldPosition(map, tile);
                    float harbourDistanceSq = TileData.FlatDistanceSq(candidate, harbourPosition);
                    if (harbourDistanceSq > maxDistanceSq)
                        continue;

                    float preferredDistanceSq = TileData.FlatDistanceSq(candidate, preferredPosition);
                if (preferredDistanceSq > bestPreferredDistanceSq)
                    continue;

                if (Mathf.Approximately(preferredDistanceSq, bestPreferredDistanceSq) && harbourDistanceSq >= bestHarbourDistanceSq)
                    continue;

                bestPreferredDistanceSq = preferredDistanceSq;
                bestHarbourDistanceSq = harbourDistanceSq;
                spawnPosition = candidate;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Indique si le type d'unité correspond à un bateau.
    /// </summary>
    private bool IsBoatType(UnitsType type)
    {
        switch (type)
        {
            case UnitsType.Fregate:
            case UnitsType.Destroyer:
            case UnitsType.Transport:
                return true;
            default:
                return false;
        }
    }
}
