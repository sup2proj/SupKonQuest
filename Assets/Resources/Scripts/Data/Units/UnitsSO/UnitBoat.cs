using UnityEngine;

[CreateAssetMenu(fileName = "UnitBoat", menuName = "Scriptable Objects/UnitBoat")]
public class UnitBoat : UnitCombatData
{
    [Header("Transport")]
    [Min(0)] public float maxTransportCapacity;
    
}
