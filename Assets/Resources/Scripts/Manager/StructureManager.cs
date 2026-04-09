using UnityEngine;
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

    public bool SpawnUnitByTypeAtPosition(int playerId, UnitsType type, float x, float z, bool isPoweredUnit, bool isProtector)
    {
        UnitData data = unitData.Find(d => d.type == type);
        if (data == null)
        {
            return false;
        }

        if (!unitPrefabDict.TryGetValue(type, out GameObject prefab) || prefab == null)
        {
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
        Vector3 position = new Vector3(x, 0, z - 3);
        GameObject unitGO = Instantiate(prefab, position, Quaternion.identity);

        if (isPoweredUnit)
            ApplyColorTint(unitGO, new Color(1f, 0.35f, 0.35f, 1f));

        if (isProtector)
            ApplyColorTint(unitGO, new Color(0.35f, 1f, 0.35f, 1f));

        UnitInstance instance = unitGO.GetComponent<UnitInstance>();
        if (instance == null)
        {
            Destroy(unitGO);
            return false;
        }

        instance.Initialize(runtimeData);
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
