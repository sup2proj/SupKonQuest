using UnityEngine;

public class Victory : MonoBehaviour
{
    [SerializeField, Min(1)] private int territoriesToWin = 5;

    public bool HasWinner { get; private set; }
    public int WinnerPlayerId { get; private set; } = -1;

    private void Update()
    {
        if (HasWinner)
            return;

        if (TryGetSoloPlayerId(out int soloPlayerId))
        {
            DeclareWinner(soloPlayerId, "il ne reste plus qu'un seul joueur");
            return;
        }

        TryDeclareWinnerByTerritories();
    }

    public bool TryDeclareWinnerByTerritories()
    {
        if (HasWinner)
            return false;

        if (TerritoryControlUtility.TryGetWinnerByTerritories(territoriesToWin, out int territoryWinnerId))
        {
            DeclareWinner(territoryWinnerId, $"a controle {territoriesToWin} territoires");
            return true;
        }

        return false;
    }

    public bool TryDeclareWinnerIfPlayerControlsEnoughTerritories(int playerId)
    {
        if (HasWinner || playerId <= 0)
            return false;

        int controlledTerritories = TerritoryControlUtility.CountControlledTerritories(playerId);
        if (controlledTerritories < territoriesToWin)
            return false;

        DeclareWinner(playerId, $"a controle {controlledTerritories} territoires");
        return true;
    }

    private bool TryGetSoloPlayerId(out int playerId)
    {
        playerId = -1;

        var sessions = Object.FindObjectsOfType<PlayerSession>();
        if (sessions == null || sessions.Length != 1)
            return false;

        playerId = sessions[0].Id;
        return playerId > 0;
    }

    private void DeclareWinner(int playerId, string reason)
    {
        HasWinner = true;
        WinnerPlayerId = playerId;

        Debug.Log($"[Victory] Le joueur {playerId} a gagne : {reason}.");

        InterfaceInstance interfaceInstance = InterfaceInstance.Instance;
        if (interfaceInstance == null)
            interfaceInstance = FindFirstObjectByType<InterfaceInstance>(FindObjectsInactive.Include);

        if (interfaceInstance != null)
            interfaceInstance.ShowVictoryPanel(playerId);
    }
    
    public static void CheckVictoryAfterCapture(int playerIdToCheck)
    {
        Victory victory = FindFirstObjectByType<Victory>(FindObjectsInactive.Include);
        if (victory != null)
        {
            victory.TryDeclareWinnerIfPlayerControlsEnoughTerritories(playerIdToCheck);
            return;
        }

        const int defaultTerritoriesToWin = 5;
        if (TerritoryControlUtility.CountControlledTerritories(playerIdToCheck) < defaultTerritoriesToWin)
            return;

        InterfaceInstance interfaceInstance = InterfaceInstance.Instance;
        if (interfaceInstance == null)
            interfaceInstance = FindFirstObjectByType<InterfaceInstance>(FindObjectsInactive.Include);

        if (interfaceInstance != null)
            interfaceInstance.ShowVictoryPanel(playerIdToCheck);
    }
}
