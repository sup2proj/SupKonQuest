using UnityEngine;
using System.Collections.Generic;

public class Defeat : MonoBehaviour
{
    private static readonly HashSet<int> defeatedPlayers = new HashSet<int>();

    [SerializeField, Min(0.1f)] private float globalDefeatCheckInterval = 0.5f;
    private float nextGlobalDefeatCheckAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDefeatedPlayers()
    {
        defeatedPlayers.Clear();
    }

    public static bool IsPlayerDefeated(int playerId)
    {
        return playerId > 0 && defeatedPlayers.Contains(playerId);
    }

    public static bool CheckDefeatAfterCapture(int playerIdToCheck, bool showPanel = true)
    {
        if (playerIdToCheck <= 0)
            return false;

        if (defeatedPlayers.Contains(playerIdToCheck))
            return true;

        if (PlayerStillHasStructure(playerIdToCheck))
            return false;

        ApplyDefeat(playerIdToCheck);
        return true;
    }

    public static void ShowDefeatForPlayer(int playerId)
    {
        if (playerId <= 0)
            return;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager != null && playerManager.GetActivePlayerId() != playerId)
            return;

        if (InterfaceInstance.Instance != null)
            InterfaceInstance.Instance.ShowDefeatPanel(playerId);
    }

    private static bool PlayerStillHasStructure(int playerId)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        PlayerSession session = playerManager != null ? playerManager.GetSession(playerId) : null;
        if (session != null)
            return session.StructureCount > 0;

        StructureInstance[] structures = Object.FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
        if (structures == null)
            return false;

        for (int i = 0; i < structures.Length; i++)
        {
            StructureInstance structure = structures[i];
            if (structure != null && structure.playerId == playerId)
                return true;
        }

        return false;
    }

    private void Update()
    {
        if (Time.time < nextGlobalDefeatCheckAt)
            return;

        nextGlobalDefeatCheckAt = Time.time + globalDefeatCheckInterval;
        CheckDefeatForAllPlayers();
    }

    private static void CheckDefeatForAllPlayers()
    {
        PlayerSession[] sessions = Object.FindObjectsByType<PlayerSession>(FindObjectsSortMode.None);
        if (sessions == null)
            return;

        for (int i = 0; i < sessions.Length; i++)
        {
            PlayerSession session = sessions[i];
            if (session == null)
                continue;

            int playerId = session.Id;
            if (playerId <= 0)
                continue;

            // Règle globale: 0 structure = défaite.
            CheckDefeatAfterCapture(playerId, showPanel: false);
        }
    }

    private static void ApplyDefeat(int playerId)
    {
        defeatedPlayers.Add(playerId);
        Debug.Log($"[Defeat] Le joueur {playerId} a perdu : il n'a plus de structures.");

        bool defeatedPlayerWasAi = IAInstance.IsAIPlayer(playerId);
        NeutralUnits.ConvertPlayerUnitsToNeutral(playerId, defeatedPlayerWasAi);

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager != null && playerManager.GetActivePlayerId() == playerId && SelectionManager.Instance != null)
            SelectionManager.Instance.ClearCurrentSelection();

        ShowDefeatForPlayer(playerId);


        TryDeclareWinnerFromRemainingPlayers();
    }

    private static void TryDeclareWinnerFromRemainingPlayers()
    {
        PlayerSession[] sessions = Object.FindObjectsByType<PlayerSession>(FindObjectsSortMode.None);
        if (sessions == null || sessions.Length == 0)
            return;

        int aliveCount = 0;
        int lastAlivePlayerId = -1;

        for (int i = 0; i < sessions.Length; i++)
        {
            PlayerSession session = sessions[i];
            if (session == null)
                continue;

            int playerId = session.Id;
            if (playerId <= 0)
                continue;

            if (!PlayerStillHasStructure(playerId))
                continue;

            aliveCount++;
            lastAlivePlayerId = playerId;
            if (aliveCount > 1)
                return;
        }

        if (aliveCount == 1 && lastAlivePlayerId > 0)
            Victory.CheckVictoryAfterElimination(lastAlivePlayerId);
    }
}