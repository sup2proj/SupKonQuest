using UnityEngine;
using System.Collections.Generic;

public class UnitsManager : MonoBehaviour
{
    public List<UnitsData> unitsDatas;
    public GameObject unitPrefab;
    
	public void SpawnUnitByIndexAtPosition(int index, Vector3 position)
    {
        if (unitsDatas.Count == 0 || unitPrefab == null || index < 0 || index >= unitsDatas.Count)
            return;

        UnitsData data = unitsDatas[index];
        GameObject unitGO = Instantiate(unitPrefab, position, Quaternion.identity);
        InfanterieInstance instance = unitGO.GetComponent<InfanterieInstance>();
        if (instance != null)
        {
            instance.Init(data);
        }
    }
}


