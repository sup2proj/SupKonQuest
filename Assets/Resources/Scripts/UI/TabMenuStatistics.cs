using TMPro;
using UnityEngine;

public class TabMenuStatistics : MonoBehaviour
{
    [System.Serializable]
    private class PlayerStatRow
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI structureCountText;
        public TextMeshProUGUI territoryCountText;
        public TextMeshProUGUI unitsCountText;

        public void Clear()
        {
            SetText(nameText, null);
            SetColor(nameText, Color.white);
            SetText(goldText, null);
            SetText(structureCountText, null);
            SetText(territoryCountText, null);
            SetText(unitsCountText, null);
        }

        public void Apply(PlayerSession session, int playerId)
        {
            if (session == null)
            {
                Clear();
                return;
            }

            SetText(nameText, session.PlayerName);
            SetColor(nameText, PlayerManager.GetPlayerColor(playerId));
            SetText(goldText, session.Gold.ToString());
            SetText(structureCountText, session.StructureCount.ToString());
            SetText(territoryCountText, TerritoryControlUtility.CountControlledTerritories(session.Id).ToString());
            SetText(unitsCountText, session.UnitCount.ToString());
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static void SetColor(TextMeshProUGUI text, Color color)
        {
            if (text != null)
                text.color = color;
        }
    }

    [Header("Players statistics rows (1 à 8)")]
    [SerializeField] private PlayerStatRow[] playerRows = new PlayerStatRow[8];

    [Header("Runtime (auto si vide)")]
    [SerializeField] private PlayerManager playerManager;

    private void Awake()
    {
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        for (int i = 0; i < playerRows.Length; i++)
        {
            PlayerStatRow row = playerRows[i];
            if (row == null)
                continue;

            int playerId = i + 1;
            PlayerSession session = playerManager != null ? playerManager.GetSession(playerId) : null;
            row.Apply(session, playerId);
        }
    }
}
