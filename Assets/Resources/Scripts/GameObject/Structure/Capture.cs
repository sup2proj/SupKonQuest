public partial class StructureInstance
{
    /// <summary>
    /// Capture la structure pour un nouveau propriétaire et met à jour les effets associés.
    /// </summary>
    private void CaptureStructure(int newOwnerId, int previousOwnerId, bool checkDefeat = true)
    {
          if (newOwnerId <= 0 || newOwnerId == playerId)
              return;

         playerId = newOwnerId;
         currentHealth = health;

         if (healthBar != null)
             healthBar.SetHealth(currentHealth);

         ApplyTerritoryName(territoryName);
         UpdateStructureCounts(previousOwnerId, newOwnerId);

         if (checkDefeat)
         {
             bool previousOwnerDefeated = Defeat.CheckDefeatAfterCapture(previousOwnerId, showPanel: false);
             if (previousOwnerDefeated)
             {
                 bool winnerDeclared = Victory.CheckVictoryAfterElimination(newOwnerId);
                 if (!winnerDeclared)
                     winnerDeclared = Victory.CheckVictoryAfterCapture(newOwnerId);
             }
             else
             {
                 Victory.CheckVictoryAfterCapture(newOwnerId);
             }
         }

         if (currentlySelected == this && PlayerManager.Instance != null && PlayerManager.Instance.GetActivePlayerId() != playerId)
             UnSelected();

         StructureManager structureManager = StructureManager.Instance;
         if (structureManager == null)
             structureManager = FindFirstObjectByType<StructureManager>();

         if (structureManager != null)
         {
             structureManager.SpawnUnitByTypeAtPosition(
                 newOwnerId,
                 UnitsType.Heavy,
                 transform.position.x,
                 transform.position.z,
                 false,
                 true,
                 this
             );
         }
    }

    /// <summary>
    /// Met à jour les compteurs de structures entre l'ancien et le nouveau propriétaire.
    /// </summary>
    private void UpdateStructureCounts(int previousOwnerId, int newOwnerId)
    {
        if (previousOwnerId == newOwnerId)
            return;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        if (playerManager == null)
            return;

        PlayerSession previousOwnerSession = previousOwnerId > 0 ? playerManager.GetSession(previousOwnerId) : null;
        if (previousOwnerSession != null)
            previousOwnerSession.removeStructure(1);

        PlayerSession newOwnerSession = newOwnerId > 0 ? playerManager.GetSession(newOwnerId) : null;
        if (newOwnerSession != null)
            newOwnerSession.AddStructure(1);

        if (StatisticsInterface.Instance != null)
            StatisticsInterface.Instance.Refresh();
    }
}
