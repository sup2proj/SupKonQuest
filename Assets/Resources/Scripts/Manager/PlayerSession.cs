using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    [SerializeField] private int id;
    [SerializeField] private int gold;
    [SerializeField] private int unitCount;
    [SerializeField] private int structureCount;

    public int Id => id;
    public int Gold => gold;
    public int UnitCount => unitCount;
    public int StructureCount => structureCount;

    public void Init(int sessionId, int startGold, int startUnitCount, int startStructureCount)
    {
        id = sessionId;
        gold = startGold;
        unitCount = startUnitCount;
        structureCount = startStructureCount;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount > 0) gold += amount;
    }

    public void AddUnit(int amount = 1)
    {
        unitCount += amount;
    }

    public void AddStructure(int amount = 1)
    {
        structureCount += amount;
    }
}