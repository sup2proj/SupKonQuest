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

        if (TerritoryControlUtility.TryGetWinnerByTerritories(territoriesToWin, out int territoryWinnerId))
        {
            DeclareWinner(territoryWinnerId, $"a contrôlé {territoriesToWin} territoires");
        }
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

        Debug.Log($"[Victory] Le joueur {playerId} a gagné : {reason}.");
    }
}
