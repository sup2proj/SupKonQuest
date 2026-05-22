using UnityEngine;

public partial class InterfaceInstance
{
    private void WireBoatExitButtonClick()
    {
        WireImageButton(boatExitButton, OnBoatExitClicked);
    }

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

    public void ShowBoatExitIcons()
    {
        SetImageActive(boatExitButton, true);
    }

    public void HideBoatExitIcons()
    {
        SetImageActive(boatExitButton, false);
    }
}