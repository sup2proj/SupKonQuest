using UnityEngine;

[CreateAssetMenu(fileName = "HealerData", menuName = "Scriptable Objects/HealerData")]
public class HealerData : ScriptableObject
{
    [Header("Economy")] 
    [Min(0)] public int price;

    [Header("Health")] 
    [Min(0)] public float maxHealth;

    [Header("Buff")] 
    [Min(0)] public float buffHealing;

    [Header("Buff Settings")] 
    [Min(0)] public float buffHealingCooldown;
    [Min(0)] public float buffTime;
    [Min(0)] public float buffRange;

    [Header("Movement")] 
    [Min(0)] public float speed;

    [Header("Production")] 
    [Min(0)] public float creationTime;

    [Header("Meta")] public UnitsType type;
    public bool isPoweredUnit = false;
}
