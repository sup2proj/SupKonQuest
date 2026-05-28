using UnityEngine;
using UnityEngine.UI;

public partial class InterfaceInstance
{
    /// <summary>
    /// Affiche l'interface de création d'unités pour la structure sélectionnée si le joueur n'est pas défait.
    /// </summary>
    public void showInterfaceForStructure()
    {
        int activePlayerId = GetSelectedPlayerId();
        if (Defeat.IsPlayerDefeated(activePlayerId))
            return;

        SetGameObjectActive(unitsQueue, true);
        SetGameObjectActive(unitsProtector, true);
        hideUnitsProtectorSlots();
        displayedStructure = StructureInstance.CurrentlySelected;
        RefreshQueueSlotsVisibility();
        RefreshProgressBarVisibility();
    }

    /// <summary>
    /// Masque l'interface de création d'unités liée à la structure et réinitialise les éléments visuels associés.
    /// </summary>
    public void HideStructureInterface()
    {
        SetGameObjectActive(unitsQueue, false);
        SetGameObjectActive(unitsProtector, false);
        SetAllQueueSlotsActive(false);
        hideUnitsProtectorSlots();
        HideProgressBarVisual(resetProgress: false);
    }

    /// <summary>
    /// Masque tous les emplacements d'UI destinés aux protecteurs d'unités.
    /// </summary>
    private void hideUnitsProtectorSlots()
    {
        SetImageArrayActive(unitsProtectorSlots, false);
    }

    /// <summary>
    /// Attache les callbacks de clic pour chaque slot de protector afin de pouvoir spawn un protector via l'UI.
    /// </summary>
    private void WireProtectorSlotClicks()
    {
        if (unitsProtectorSlots == null)
            return;

        for (int i = 0; i < unitsProtectorSlots.Length; i++)
        {
            var img = unitsProtectorSlots[i];
            int capturedIndex = i;
            WireImageButton(img, () => SpawnStructProtectorOnInterface(capturedIndex));
        }
    }

    /// <summary>
    /// Initialise et paye la création d'une unité : débite l'or du joueur, ajoute la requête à la file
    /// et démarre la coroutine de traitement si nécessaire.
    /// Retourne true si la création a été acceptée.
    /// </summary>
    public bool InitUnitsCreation(int unitIndex, UnitsType type, float x, float z, bool isPoweredUnit, bool isProtector)
    {
        int playerId = GetSelectedPlayerId();
        if (Defeat.IsPlayerDefeated(playerId))
            return false;

        var actionInterface = ActionInterface.Instance;
        UnitData unitData = actionInterface.unitDatas[unitIndex];
        float multiplier = isPoweredUnit ? 1.20f : 1f;
        int cost = Mathf.RoundToInt(unitData.price * multiplier);
        cost = Mathf.Max(0, cost);

        ResolvePlayerManager();
        if (playerManager == null)
            return false;

        PlayerSession session = playerManager.GetSession(playerId);
        if (session == null)
            return false;

        if (!session.SpendGold(cost))
        {
            Debug.Log($"[InterfaceInstance] Pas assez d'or pour demander la création: joueur={playerId}, gold={session.Gold}, coût={cost}.", this);
            RefreshPlayerStatisticsUI();
            return false;
        }

        Debug.Log($"[InterfaceInstance] Création demandée et payée: joueur={playerId}, coût={cost}, goldRestant={session.Gold}, type={type}, powered={isPoweredUnit}.", this);

        int buildingPlayerId = playerId;
        var selectedStructure = StructureInstance.CurrentlySelected;
        int sourceStructureId = -1;
        if (selectedStructure != null)
        {
            buildingPlayerId = selectedStructure.playerId;
            sourceStructureId = selectedStructure.GetInstanceID();
        }

        var creationState = GetOrCreateCreationState(sourceStructureId);
        creationState.queue.Enqueue(new UnitCreationRequest
        {
            unitIndex = unitIndex,
            type = type,
            x = x,
            z = z,
            isPoweredUnit = isPoweredUnit,
            isProtector = isProtector,
            buildingPlayerId = buildingPlayerId,
            sourceStructureId = sourceStructureId,
        });

        if (creationState.coroutine == null)
            creationState.coroutine = StartCoroutine(ProcessCreationQueue(sourceStructureId));

        RefreshPlayerStatisticsUI();
        return true;
    }

    /// <summary>
    /// Gère la demande de spawn d'un protector depuis l'interface en utilisant le slot cliqué.
    /// </summary>
    private void SpawnStructProtectorOnInterface(int slotIndex)
    {
        ProtectorSlotToType.TryGetValue(slotIndex, out var type);
        var selected = StructureInstance.CurrentlySelected;
        Vector3 pos = selected.StructurePosition;
        bool accepted = InitUnitsCreation(slotIndex, type, pos.x + 1f, pos.z + 1f, false, true);
        // Correspondance UI car les icones ne suivent pas l'ordre des slots.
        int iconButtonIndex;
        if (!ProtectorSlotToIconButtonNumber.TryGetValue(slotIndex, out iconButtonIndex))
            iconButtonIndex = slotIndex;

        GameObject clickedImageGO = ActionInterface.Instance.GetClickedUnitsIcon(iconButtonIndex, false);
        if (accepted && clickedImageGO != null)
        {
            addUnitToQueue(clickedImageGO);
        }
    }

    /// <summary>
    /// Affiche sur l'interface les unités/protectors disponibles pour la structure selon le type et le statut powered.
    /// </summary>
    public void showUnitsNextToStructure(UnitsType type, bool isPoweredUnit)
    {
        WireProtectorSlotClicks();
        ProtectorUiMapping mapping;
        if (!ProtectorUiByType.TryGetValue(type, out mapping))
            return;

        if (unitsProtectorSlots == null || mapping.slotIndex < 0 || mapping.slotIndex >= unitsProtectorSlots.Length)
            return;

        SetImageActive(unitsProtectorSlots[mapping.slotIndex], true);
        ActionInterface.Instance.ShowUnitProtectorPrice(mapping.unitDataIndex, mapping.priceIndex);
    }
}