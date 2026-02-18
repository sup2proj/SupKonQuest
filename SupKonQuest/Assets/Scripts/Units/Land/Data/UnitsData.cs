using UnityEngine;

[CreateAssetMenu(
    fileName = "UnitsData",
    menuName = "Scriptable Objects/UnitsData"
)]
public class UnitsData : ScriptableObject
{
    [Header("Economy")]
    [Min(0)] public int price;

    [Header("Health")]
    [Min(0)] public float maxHealth;
    [Min(0)] public float health;
    

    [Header("Attack")]
    [Min(0)] public float attack;
    [Min(0)] public float attackSpeed;
    [Min(0)] public float attackRange;

    [Header("Movement")]
    [Min(0)] public float speed;

    [Header("Production")]
    [Min(0)] public float creationTime;

    [Header("Meta")]
    public UnitsType type;
    public bool isPoweredUnit = false;
}