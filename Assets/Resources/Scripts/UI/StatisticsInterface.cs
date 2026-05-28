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

    /// <summary>
    /// Récupère la référence vers le gestionnaire de joueurs au chargement.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();
    }

    /// <summary>
    /// Met à jour les statistiques affichées pour le joueur actif.
    /// </summary>
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
            Debug.LogWarning($"[PlayerStatisticsPresenter] Session introuvable pour playerId={playerId}.", this);
            return;
        }

        goldText.text = session.Gold.ToString();
        unitCountText.text = session.UnitCount.ToString();
        structureCountText.text = session.StructureCount.ToString();

        int territoryCount = global::TerritoryControlUtility.CountControlledTerritories(playerId);

        if (territoryCountText != null)
            territoryCountText.text = territoryCount.ToString();
    }
}