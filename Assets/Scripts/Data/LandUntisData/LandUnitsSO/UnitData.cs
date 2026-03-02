using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("Economy")]
    [Min(0)] public int price;

    [Header("Health")]
    [Min(0)] public float maxHealth;

    [Header("Movement")]
    [Min(0)] public float speed;

    [Header("Production")]
    [Min(0)] public float creationTime;

    [Header("Combat (Optional)")]
    public bool canAttack;
    [Min(0)] public float attack;
    [Min(0)] public float attackSpeed;
    [Min(0)] public float attackRange;

    [Header("Heal (Optional)")]
    public bool canHeal;
    [Min(0)] public float healAmount;
    [Min(0)] public float healCooldown;
    [Min(0)] public float healDuration;
    [Min(0)] public float healRange;

    [Header("Support / Buff (Optional)")]
    public bool canBuff;

    [Min(0)] public float buffAttackSpeed;
    [Min(0)] public float buffAttackSpeedCooldown;

    [Min(0)] public float buffSpeed;
    [Min(0)] public float buffSpeedCooldown;

    [Min(0)] public float buffDamage;
    [Min(0)] public float buffDamageCooldown;

    [Min(0)] public float buffDuration;
    [Min(0)] public float buffRange;

    [Header("Meta")]
    public UnitsType type;
    public bool isPoweredUnit;
}