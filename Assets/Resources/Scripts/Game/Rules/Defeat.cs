using UnityEngine;
using System.Collections.Generic;

public class Defeat : MonoBehaviour
{
    private static readonly HashSet<int> defeatedPlayers = new HashSet<int>();

    [SerializeField, Min(0.1f)] private float globalDefeatCheckInterval = 0.5f;
    private float nextGlobalDefeatCheckAt;

    /// <summary>
    /// Réinitialise l'ensemble des joueurs défaits lors du rechargement du sous-système.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDefeatedPlayers()
    {
        defeatedPlayers.Clear();
    }

    /// <summary>
    /// Indique si le joueur donné est marqué comme défait.
    /// </summary>
    public static bool IsPlayerDefeated(int playerId)
    {
        return playerId > 0 && defeatedPlayers.Contains(playerId);
    }

    /// <summary>
    /// Vérifie si un joueur est défait après une capture (par ex. perte de structures) et l'applique si nécessaire.
    /// </summary>
    public static bool CheckDefeatAfterCapture(int playerIdToCheck, bool showPanel = true)
    {
        if (playerIdToCheck <= 0)
            return false;

        if (defeatedPlayers.Contains(playerIdToCheck))
            return true;

        if (PlayerStillHasStructure(playerIdToCheck))
            return false;

        ApplyDefeat(playerIdToCheck, "il n'a plus de structures");
        return true;
    }

    /// <summary>
    /// Affiche le panneau de défaite pour le joueur spécifié.
    /// </summary>
    public static void ShowDefeatForPlayer(int playerId)
    {
        if (playerId <= 0)
            return;

        if (InterfaceInstance.Instance != null)
            InterfaceInstance.Instance.ShowDefeatPanel(playerId);
    }

    /// <summary>
    /// Détermine si le joueur possède encore des structures dans la scène ou via le PlayerManager.
    /// </summary>
    private static bool PlayerStillHasStructure(int playerId)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        PlayerSession session = playerManager != null ? playerManager.GetSession(playerId) : null;
        if (session != null && session.StructureCount > 0)
            return true;

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

    /// <summary>
    /// Contrôle périodiquement (intervalle configurable) l'état de défaite pour tous les joueurs.
    /// </summary>
    private void Update()
    {
        if (Time.time < nextGlobalDefeatCheckAt)
            return;

        nextGlobalDefeatCheckAt = Time.time + globalDefeatCheckInterval;
        CheckDefeatForAllPlayers();
    }

    /// <summary>
    /// Parcourt toutes les sessions de joueurs et vérifie si elles doivent être marquées comme défaites.
    /// </summary>
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

    /// <summary>
    /// Applique la défaite à un joueur : convertit ses unités en neutres, nettoie la sélection et notifie.
    /// </summary>
    private static void ApplyDefeat(int playerId, string reason)
    {
        defeatedPlayers.Add(playerId);
        Debug.Log($"[Defeat] Le joueur {playerId} a perdu : {reason}.");

        bool defeatedPlayerWasAi = IAInstance.IsAIPlayer(playerId);
        NeutralUnits.ConvertPlayerUnitsToNeutral(playerId, defeatedPlayerWasAi);

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager != null && playerManager.GetActivePlayerId() == playerId && SelectionManager.Instance != null)
            SelectionManager.Instance.ClearCurrentSelection();

        ShowDefeatForPlayer(playerId);
    }
}