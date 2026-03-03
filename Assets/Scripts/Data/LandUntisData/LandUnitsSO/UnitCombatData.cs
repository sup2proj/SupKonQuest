using UnityEngine;

[CreateAssetMenu(fileName = "UnitCombatData", menuName = "Scriptable Objects/UnitCombatData")]
public class UnitCombatData : UnitData
{
    [Header("Combat")]
    [Min(0)] public float attack;
    [Min(0)] public float attackSpeed;
    [Min(0)] public float attackRange;
}
