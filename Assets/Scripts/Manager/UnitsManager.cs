using UnityEngine;
using System.Collections.Generic;

public class UnitsManager : MonoBehaviour
{
    public List<UnitsData> unitsData;
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
        if (unitsData.Count == 0 || unitPrefab == null || index < 0 || index >= unitsData.Count)
            return;

        UnitsData data = unitsData[index];
        GameObject unitGO = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitsSoldierInstance instance = unitGO.GetComponent<UnitsSoldierInstance>();
        if (instance != null)
        {
            instance.Init(data);
        }
    }
    
    public void SpawnUnitByTypeAtPosition(UnitsType type, Vector3 position)
    {
        int index = unitsData.FindIndex(data => data.type == type);
        if (index != -1)
        {
            SpawnUnitByIndexAtPosition(index, position);
        }
    }
    
    
}


