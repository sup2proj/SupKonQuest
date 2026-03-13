using UnityEngine;

[CreateAssetMenu(fileName = "StructureData", menuName = "Scriptable Objects/StructureData")]
public class StructureData : ScriptableObject
{
    [Header("Type")]
    [Min(0)] public bool neutralStructure;
    public StructureType structureType;
    
    [Header("Units")]
    public UnitsType unitsProtectorType;
    public bool isAlive;

    [Header("Player")] 
    public PlayerNumber player;
}