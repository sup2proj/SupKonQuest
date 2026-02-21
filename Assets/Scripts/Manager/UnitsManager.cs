using UnityEngine;
using System.Collections.Generic;

public class UnitsManager : MonoBehaviour
{
    public List<UnitsData> unitsDatas;
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
        if (unitsDatas.Count == 0 || unitPrefab == null || index < 0 || index >= unitsDatas.Count)
            return;

        UnitsData data = unitsDatas[index];
        GameObject unitGO = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitsSoldierInstance instance = unitGO.GetComponent<UnitsSoldierInstance>();
        if (instance != null)
        {
            instance.Init(data);
        }
    }
    
    public void SpawnUnitByTypeAtPosition(UnitsType type, Vector3 position)
    {
        int index = unitsDatas.FindIndex(data => data.type == type);
        if (index != -1)
        {
            SpawnUnitByIndexAtPosition(index, position);
        }
    }
    
    
}


