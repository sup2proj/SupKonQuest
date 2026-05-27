using UnityEngine;

public partial class InterfaceInstance
{
    /// <summary>
    /// Attache le callback de clic au bouton de sortie des bateaux.
    /// </summary>
    private void WireBoatExitButtonClick()
    {
        WireImageButton(boatExitButton, OnBoatExitClicked);
    }

    /// <summary>
    /// Gère l'action de débarquement des unités depuis les bateaux sélectionnés appartenant au joueur actif.
    /// </summary>
    private void OnBoatExitClicked()
    {
        int activePlayerId = PlayerManager.Instance != null ? PlayerManager.Instance.GetActivePlayerId() : GetSelectedPlayerId();
        if (Defeat.IsPlayerDefeated(activePlayerId))
            return;

        int unloadedBoats = 0;

        if (SelectionManager.Instance != null && SelectionManager.Instance.CurrentlySelectedObjects != null)
        {
            for (int i = 0; i < SelectionManager.Instance.CurrentlySelectedObjects.Count; i++)
            {
                SelectableObject selectable = SelectionManager.Instance.CurrentlySelectedObjects[i];
                if (selectable == null)
                    continue;

                UnitInstance unit = selectable.GetComponent<UnitInstance>();
                if (unit == null || unit.playerId != activePlayerId)
                    continue;

                if (!BoatTransport.IsBoatUnit(unit))
                    continue;

                if (BoatTransport.ExitAllUnits(unit))
                {
                    unloadedBoats = 1;
                    break;
                }
            }
        }

        Debug.Log($"[InterfaceInstance] BoatExit: {unloadedBoats} bateau(x) ont debarque leurs unites.");
    }

    /// <summary>
    /// Affiche l'icône permettant d'ordonner la sortie des unités des bateaux.
    /// </summary>
    public void ShowBoatExitIcons()
    {
        SetImageActive(boatExitButton, true);
    }

    /// <summary>
    /// Masque l'icône de sortie des bateaux.
    /// </summary>
    public void HideBoatExitIcons()
    {
        SetImageActive(boatExitButton, false);
    }
}