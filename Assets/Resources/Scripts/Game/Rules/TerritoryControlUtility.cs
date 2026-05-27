using System.Collections.Generic;
using UnityEngine;

public static class TerritoryControlUtility
{
    /// <summary>
    /// Retourne le nombre de territoires entièrement contrôlés par le joueur spécifié.
    /// </summary>
    public static int CountControlledTerritories(int playerId)
    {
        if (playerId <= 0)
            return 0;

        if (!TryGetControlledTerritoriesByOwner(out var territoriesByOwner))
            return 0;

        territoriesByOwner.TryGetValue(playerId, out int controlledTerritories);
        return controlledTerritories;
    }

    /// <summary>
    /// Cherche un joueur qui contrôle au moins <paramref name="territoriesToWin"/> territoires.
    /// Renvoie true et l'ID du joueur gagnant si trouvé.
    /// </summary>
    public static bool TryGetWinnerByTerritories(int territoriesToWin, out int playerId)
    {
        playerId = -1;

        if (territoriesToWin <= 0)
            return false;

        if (!TryGetControlledTerritoriesByOwner(out var territoriesByOwner))
            return false;

        foreach (var kvp in territoriesByOwner)
        {
            if (kvp.Value >= territoriesToWin)
            {
                playerId = kvp.Key;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calcule et retourne un dictionnaire mapping ownerId => nombre de territoires entièrement contrôlés.
    /// Renvoie false si aucune donnée valable n'a pu être déterminée.
    /// </summary>
    private static bool TryGetControlledTerritoriesByOwner(out Dictionary<int, int> territoriesByOwner)
    {
        territoriesByOwner = new Dictionary<int, int>();

        var allStructures = Object.FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
        if (allStructures == null || allStructures.Length == 0)
            return false;

        var totalPerTerritory = new Dictionary<int, int>();
        var ownerCounts = new Dictionary<int, Dictionary<int, int>>();

        foreach (var structure in allStructures)
        {
            if (structure == null)
                continue;

            int territoryId = structure.territoryId;
            if (territoryId <= 0)
                continue;

            totalPerTerritory.TryGetValue(territoryId, out int currentTotal);
            totalPerTerritory[territoryId] = currentTotal + 1;

            if (!ownerCounts.TryGetValue(territoryId, out var ownersDict))
            {
                ownersDict = new Dictionary<int, int>();
                ownerCounts[territoryId] = ownersDict;
            }

            int ownerId = structure.playerId;
            ownersDict.TryGetValue(ownerId, out int ownerCount);
            ownersDict[ownerId] = ownerCount + 1;
        }

        foreach (var kvp in totalPerTerritory)
        {
            int territoryId = kvp.Key;
            int totalStructures = kvp.Value;

            if (!ownerCounts.TryGetValue(territoryId, out var owners))
                continue;

            int controllingPlayerId = -1;
            foreach (var owner in owners)
            {
                if (owner.Value == totalStructures)
                {
                    controllingPlayerId = owner.Key;
                    break;
                }
            }

            if (controllingPlayerId <= 0)
                continue;

            territoriesByOwner.TryGetValue(controllingPlayerId, out int controlledTerritories);
            territoriesByOwner[controllingPlayerId] = controlledTerritories + 1;
        }

        return territoriesByOwner.Count > 0;
    }
}

