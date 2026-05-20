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

    [Header("Meta")]
    public UnitsType type;
    public bool isPoweredUnit;
    public bool isProtector;
    public bool isNeutral;
    public int playerId;
}