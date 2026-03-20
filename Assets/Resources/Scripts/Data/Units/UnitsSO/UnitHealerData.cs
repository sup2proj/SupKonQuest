using UnityEngine;

[CreateAssetMenu(fileName = "UnitHealerData", menuName = "Scriptable Objects/UnitHealerData")]
public class UnitHealerData : UnitData
{
    [Header("Heal")]
    [Min(0)] public float healAmount;
    [Min(0)] public float healCooldown;
    [Min(0)] public float healDuration;
    [Min(0)] public float healRange;
}
