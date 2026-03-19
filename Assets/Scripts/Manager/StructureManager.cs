using UnityEngine;
using System.Collections.Generic;

public class StructureManager : MonoBehaviour
{
    public static StructureManager Instance;
    public List<UnitData> unitData;

    [Header("Unit Prefabs")]
    [SerializeField] private List<UnitPrefabMapping> unitPrefabMappings = new List<UnitPrefabMapping>();
    private Dictionary<UnitsType, GameObject> unitPrefabDict = new Dictionary<UnitsType, GameObject>();

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

    public void SpawnUnitByTypeAtPosition(UnitsType type, float x, float z, bool isPoweredUnit, bool isProtector)
    {
        UnitData data = unitData.Find(d => d.type == type);
        if (data == null)
        {
            Debug.LogError($"[StructureManager] UnitData introuvable pour type={type}");
            return;
        }

        if (!unitPrefabDict.TryGetValue(type, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[StructureManager] Prefab introuvable pour type={type} (vérifie unitPrefabMappings)");
            return;
        }

        Vector3 position = new Vector3(x, 0, z - 3);

        // Instanciation GO d'abord (nécessaire pour teinter/couleur)
        GameObject unitGO = Instantiate(prefab, position, Quaternion.identity);

        UnitData runtimeData = data;
        if (isPoweredUnit)
        {
            runtimeData = Instantiate(data);
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

            ApplyColorTint(unitGO, new Color(1f, 0.35f, 0.35f, 1f));
        }

        if (isProtector)
        {
            runtimeData.isProtector = true;
            ApplyColorTint(unitGO, new Color(0.35f, 1f, 0.35f, 1f));
        }

        UnitInstance instance = unitGO.GetComponent<UnitInstance>();
        if (instance == null)
        {
            Debug.LogError($"[StructureManager] Le prefab pour {type} n'a pas de composant UnitInstance.");
            Destroy(unitGO);
            return;
        }
        instance.Initialize(runtimeData);
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
