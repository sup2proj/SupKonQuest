using TMPro;
using UnityEngine;

/// <summary>
/// Présente les statistiques du joueur actuellement sélectionné dans l'UI.
/// Source du joueur actif: InterfaceInstance.ActivePlayerNumber.
/// Source des données: PlayerManager -> PlayerSession.
/// </summary>
public class StatisticsInterface : MonoBehaviour
{
    public static StatisticsInterface Instance;
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI unitCountText;
    [SerializeField] private TextMeshProUGUI structureCountText;

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
            Debug.LogWarning($"[PlayerStatisticsPresenter] Session introuvable pour playerId={playerId}.", this);
            return;
        }

        goldText.text = session.Gold.ToString();
        unitCountText.text = session.UnitCount.ToString();
        structureCountText.text = session.StructureCount.ToString();
    }
}