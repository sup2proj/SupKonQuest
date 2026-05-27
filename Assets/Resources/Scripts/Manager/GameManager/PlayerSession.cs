using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    [SerializeField] public int id;
    [SerializeField] public string playerName;
    [SerializeField] public int gold;
    [SerializeField] public int unitCount;
    [SerializeField] public int structureCount;

    public int Id => id;
    public string PlayerName => playerName;
    public int Gold => gold;
    public int UnitCount => unitCount;
    public int StructureCount => structureCount;

    /// <summary>
    /// Initialise la session avec les valeurs de départ du joueur.
    /// </summary>
    public void Init(int sessionId, int startGold, int startUnitCount, int startStructureCount)
    {
        id = sessionId;
        playerName = $"Player {sessionId}";
        gold = startGold;
        unitCount = startUnitCount;
        structureCount = startStructureCount;
    }

    /// <summary>
    /// Dépense de l'or si la session possède suffisamment de ressources.
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        return true;
    }

    /// <summary>
    /// Ajoute de l'or à la session.
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount > 0) gold += amount;
    }

    /// <summary>
    /// Incrémente le nombre d'unités contrôlées par la session.
    /// </summary>
    public void AddUnit(int amount)
    {
        unitCount += amount;
    }

  /// <summary>
  /// Décrémente le nombre d'unités contrôlées par la session.
  /// </summary>
  public void removeUnit(int amount)
    {
        unitCount -= amount;
    }

    /// <summary>
    /// Incrémente le nombre de structures contrôlées par la session.
    /// </summary>
    public void AddStructure(int amount)
    {
        structureCount += amount;
    }

    /// <summary>
    /// Décrémente le nombre de structures contrôlées par la session sans descendre sous zéro.
    /// </summary>
    public void removeStructure(int amount)
    {
        structureCount = Mathf.Max(0, structureCount - amount);
    }
}