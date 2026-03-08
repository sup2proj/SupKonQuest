using UnityEngine;
using System.Collections.Generic;

public class StructureManager : MonoBehaviour
{
    public static StructureManager Instance;
    public List<UnitData> unitData;
    [Header("Unit Prefabs")]
    [SerializeField] private List<UnitPrefabMapping> unitPrefabMappings = new List<UnitPrefabMapping>();
    private Dictionary<UnitsType, GameObject> unitPrefabDict = new Dictionary<UnitsType, GameObject>();
    
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
    
    public void SpawnUnitByTypeAtPosition(UnitsType type, float x, float z)
    {
        UnitData data = unitData.Find(d => d.type == type);
        GameObject prefab = unitPrefabDict[type];
        Vector3 position = new Vector3(x, 0, z-3);
        
        GameObject unitGO = Instantiate(prefab, position, Quaternion.identity);
        UnitInstance instance = unitGO.GetComponent<UnitInstance>();
        instance.Initialize(data);
    }
    
    
}


