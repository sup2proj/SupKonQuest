using UnityEngine;

[CreateAssetMenu(fileName = "UnitSupportData", menuName = "Scriptable Objects/UnitSupportData")]
public class UnitSupportData : UnitData
{
    [Header("Support / Buff")]
    [Min(0)] public float buffAttackSpeed;

    [Min(0)] public float buffSpeed;

    [Min(0)] public float buffDamage;
    [Min(0)] public float buffCooldown;

    [Min(0)] public float buffDuration;
    [Min(0)] public float buffRange;
}
