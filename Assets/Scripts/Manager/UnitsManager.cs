using UnityEngine;
using System.Collections.Generic;

public class UnitsManager : MonoBehaviour
{
    public List<UnitData> unitData;
    public UnitsType unitsType;
    public GameObject unitPrefab;
    
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
    
    public void SpawnUnitByTypeAtPosition(UnitsType type, Vector3 position)
    {
        int index = unitData.FindIndex(data => data.type == type);
        if (index != -1)
        {
            SpawnUnitByIndexAtPosition(index, position);
        }
    }
    
    
}


