using UnityEngine;

public class Victory : MonoBehaviour
{
    [SerializeField, Min(1)] private int territoriesToWin = 5;

    public bool HasWinner { get; private set; }
    public int WinnerPlayerId { get; private set; } = -1;

    /// <summary>
    /// Vérifie chaque frame si une condition de victoire est remplie (dernier joueur ou par territoires).
    /// </summary>
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

    /// <summary>
    /// Tente de déclarer un gagnant si un joueur contrôle suffisamment de territoires.
    /// </summary>
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

    /// <summary>
    /// Vérifie si le joueur donné contrôle assez de territoires pour gagner et le déclare si c'est le cas.
    /// </summary>
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

    /// <summary>
    /// Déclare le joueur comme gagnant s'il est le dernier joueur possédant des structures.
    /// </summary>
    public bool TryDeclareWinnerIfOnlyPlayerWithStructures(int playerId)
    {
        if (HasWinner || playerId <= 0)
            return false;

        DeclareWinner(playerId, "il est le dernier joueur avec des structures");
        return true;
    }

    /// <summary>
    /// Tente de récupérer l'ID du seul joueur restant (si un seul PlayerSession existe).
    /// </summary>
    private bool TryGetSoloPlayerId(out int playerId)
    {
        playerId = -1;

        var sessions = Object.FindObjectsByType<PlayerSession>(FindObjectsSortMode.None);
        if (sessions == null || sessions.Length != 1)
            return false;

        playerId = sessions[0].Id;
        return playerId > 0;
    }

    /// <summary>
    /// Déclare formellement le gagnant, met à jour l'état et affiche le panneau de victoire.
    /// </summary>
    private void DeclareWinner(int playerId, string reason)
    {
        HasWinner = true;
        WinnerPlayerId = playerId;
        InterfaceInstance interfaceInstance = InterfaceInstance.Instance;
        if (interfaceInstance == null)
            interfaceInstance = FindFirstObjectByType<InterfaceInstance>(FindObjectsInactive.Include);

        if (interfaceInstance != null)
            interfaceInstance.ShowVictoryPanel(playerId);
    }
    
    /// <summary>
    /// Vérifie la condition de victoire après la capture d'un territoire pour le joueur spécifié.
    /// Si aucune instance Victory n'existe, applique une logique par défaut.
    /// </summary>
    public static bool CheckVictoryAfterCapture(int playerIdToCheck)
    {
        Victory victory = FindFirstObjectByType<Victory>(FindObjectsInactive.Include);
        if (victory != null)
            return victory.TryDeclareWinnerIfPlayerControlsEnoughTerritories(playerIdToCheck);

        const int defaultTerritoriesToWin = 5;
        if (TerritoryControlUtility.CountControlledTerritories(playerIdToCheck) < defaultTerritoriesToWin)
            return false;

        InterfaceInstance interfaceInstance = InterfaceInstance.Instance;
        if (interfaceInstance == null)
            interfaceInstance = FindFirstObjectByType<InterfaceInstance>(FindObjectsInactive.Include);

        if (interfaceInstance != null)
            interfaceInstance.ShowVictoryPanel(playerIdToCheck);

        return true;
    }

    /// <summary>
    /// Vérifie la condition de victoire après l'élimination d'un joueur (dernier joueur avec structures).
    /// Si aucune instance Victory n'existe, affiche le panneau de victoire directement.
    /// </summary>
    public static bool CheckVictoryAfterElimination(int playerIdToCheck)
    {
        Victory victory = FindFirstObjectByType<Victory>(FindObjectsInactive.Include);
        if (victory != null)
            return victory.TryDeclareWinnerIfOnlyPlayerWithStructures(playerIdToCheck);

        InterfaceInstance interfaceInstance = InterfaceInstance.Instance;
        if (interfaceInstance == null)
            interfaceInstance = FindFirstObjectByType<InterfaceInstance>(FindObjectsInactive.Include);

        if (interfaceInstance != null)
            interfaceInstance.ShowVictoryPanel(playerIdToCheck);

        return true;
    }
}
