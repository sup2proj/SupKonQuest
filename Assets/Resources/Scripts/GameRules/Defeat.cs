using UnityEngine;
using System.Collections.Generic;

public class Defeat : MonoBehaviour
{
    private static readonly HashSet<int> defeatedPlayers = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDefeatedPlayers()
    {
        defeatedPlayers.Clear();
    }

    public static bool CheckDefeatAfterCapture(int playerIdToCheck, bool showPanel = true)
    {
        if (playerIdToCheck <= 0)
            return false;

        if (defeatedPlayers.Contains(playerIdToCheck))
            return true;

        if (PlayerStillHasStructure(playerIdToCheck))
            return false;

        defeatedPlayers.Add(playerIdToCheck);
        Debug.Log($"[Defeat] Le joueur {playerIdToCheck} a perdu : il n'a plus de structures.");

        if (showPanel)
            ShowDefeatForPlayer(playerIdToCheck);

        return true;
    }

    public static void ShowDefeatForPlayer(int playerId)
    {
        InterfaceInstance interfaceInstance = InterfaceInstance.Instance;
        if (interfaceInstance == null)
            interfaceInstance = FindFirstObjectByType<InterfaceInstance>(FindObjectsInactive.Include);

        if (interfaceInstance != null)
            interfaceInstance.ShowDefeatPanel(playerId);
    }

    private static bool PlayerStillHasStructure(int playerId)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        PlayerSession session = playerManager != null ? playerManager.GetSession(playerId) : null;
        if (session != null)
            return session.StructureCount > 0;

        StructureInstance[] structures = Object.FindObjectsOfType<StructureInstance>();
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
}
