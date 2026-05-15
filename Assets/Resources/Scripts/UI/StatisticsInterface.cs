using TMPro;
using UnityEngine;

public class StatisticsInterface : MonoBehaviour
{
    public static StatisticsInterface Instance;
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI unitCountText;
    [SerializeField] private TextMeshProUGUI structureCountText;
    [SerializeField] private TextMeshProUGUI territoryCountText;

    [Header("Runtime (auto si vide)")]
    [SerializeField] private PlayerManager playerManager;

    private void Awake()
    {
        Instance = this;
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();
    }

    public void Refresh()
    {
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        int playerId = 1;
        if (InterfaceInstance.Instance != null)
            playerId = InterfaceInstance.Instance.ActivePlayerNumber;

        var session = playerManager != null ? playerManager.GetSession(playerId) : null;
        if (session == null)
        {
            Debug.LogWarning($"[PlayerStatisticsPresenter] Session introuvable pour playerId={playerId}.",this);
            return;
        }
        goldText.text = session.Gold.ToString();
        unitCountText.text = session.UnitCount.ToString();
        structureCountText.text = session.StructureCount.ToString();

        // Calcul des territoires contrôlés
        int territoryCount = CalculateOwnedTerritories(playerId);

        if (territoryCountText != null)
            territoryCountText.text = territoryCount.ToString();
    }

    private int CalculateOwnedTerritories(int playerId)
    {
        int playerTerritoriesOwned = 0;
        var allStructures = FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);

        // TerritoryId -> nombre total de structures
        var totalPerTerritory =
            new System.Collections.Generic.Dictionary<int, int>();

        // TerritoryId -> (PlayerId -> nombre de structures)
        var ownerCounts = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, int>>();

        // Comptage des structures
        foreach (var s in allStructures)
        {
            if (s == null)
                continue;
            int tid = s.territoryId;
            if (tid <= 0)
                continue;
            // Nombre total de structures du territoire
            totalPerTerritory.TryGetValue(tid, out int curTotal);
            totalPerTerritory[tid] = curTotal + 1;
            // Récupérer ou créer le dictionnaire des propriétaires
            if (!ownerCounts.TryGetValue(tid, out var ownersDict))
            {
                ownersDict = new System.Collections.Generic.Dictionary<int, int>();
                ownerCounts[tid] = ownersDict;
            }
            // Ajouter une structure au joueur propriétaire
            int owner = s.playerId;

            ownersDict.TryGetValue(owner, out int ownerCount);
            ownersDict[owner] = ownerCount + 1;
        }

        // Vérifier les territoires totalement contrôlés
        foreach (var kv in totalPerTerritory)
        {
            int tid = kv.Key;
            int total = kv.Value;

            if (ownerCounts.TryGetValue(tid, out var owners))
            {
                owners.TryGetValue(playerId, out int ownedCount);

                if (ownedCount >= total)
                    playerTerritoriesOwned++;
            }
        }

        return playerTerritoriesOwned;
    }
}