using UnityEngine;
using System.Collections.Generic;

public class StructureManager : MonoBehaviour
{
    public StructureManager Instance;
    public List<UnitData> unitData;
    public UnitsType unitsType;
    public GameObject unitPrefab;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnUnitByIndexAtPosition(0, Vector3.zero);
        }
    }
    
    
	public void SpawnUnitByIndexAtPosition(int index, Vector3 position)
    {
        if (unitData.Count == 0 || unitPrefab == null || index < 0 || index >= unitData.Count)
            return;

        UnitData data = unitData[index];
        GameObject unitGO = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitInstance instance = unitGO.GetComponent<UnitInstance>();
        if (instance != null)
        {
            instance.Initialize(data);
        }
    }
    
    public void SpawnUnitByTypeAtPosition(UnitsType type, float x, float z)
    {
        string unitPath = "Prefabs/Units/";
        int index = unitData.FindIndex(data => data.type == type);
        //logique de spawn à des coordonnées x et z 
        
        if (index != -1)
        {
            SpawnUnitByIndexAtPosition(index, position);
        }
    }
    
    
}


