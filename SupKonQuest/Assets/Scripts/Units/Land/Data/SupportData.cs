using UnityEngine;

[CreateAssetMenu(
	fileName = "Support", 
	menuName = "Scriptable Objects/Support"
)]

public class Support : ScriptableObject
{
    [Header("Economy")]
    [Min(0)] public int price;

    [Header("Health")]
    [Min(0)] public float maxHealth;

    [Header("Buff")]
    [Min(0)] public float buff;
    [Min(0)] public float buffSpeed;
    [Min(0)] public float buffRange;

    [Header("Movement")]
    [Min(0)] public float speed;

    [Header("Production")]
    [Min(0)] public float creationTime;

    [Header("Meta")]
    public UnitsType type;
    public bool isPoweredUnit = false;
}