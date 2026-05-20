using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public partial class InterfaceInstance
{
    public void addUnitToQueue(GameObject clickedUnit)
    {
        int structureId = GetCurrentStructureId();
        if (structureId == -1 || clickedUnit == null)
            return;

        var source = clickedUnit.GetComponentInChildren<Image>(true);
        if (source == null || source.sprite == null)
            return;

        var queue = GetOrCreateStructureQueue(structureId);
        if (queueSlots == null)
            return;
        if (queue.Count >= queueSlots.Length)
            return;

        queue.Add(source.sprite);
        RefreshQueueSlotsVisibility();
    }

    public void FillSlotImage(int slotIndex, GameObject clickedUnit)
    {
        if (queueSlots == null || slotIndex < 0 || slotIndex >= queueSlots.Length)
            return;

        if (clickedUnit == null)
            return;

        var target = queueSlots[slotIndex];
        if (target == null)
            return;

        var source = clickedUnit.GetComponentInChildren<Image>(true);
        if (source == null)
            return;

        target.sprite = source.sprite;
    }

    private IEnumerator ProcessCreationQueue(int structureId)
    {
        if (structureId == -1)
            yield break;

        var creationState = GetOrCreateCreationState(structureId);
        while (creationState.queue.Count > 0)
        {
            UnitCreationRequest req = creationState.queue.Dequeue();

            var actionInterface = ActionInterface.Instance;
            float creationTime = actionInterface.unitDatas[req.unitIndex].creationTime;
            creationState.hasCurrentRequest = true;
            creationState.currentCreationStartedAt = Time.time;
            creationState.currentCreationDuration = creationTime;

            if (progressBar != null)
            {
                RefreshProgressBarVisibility();
                if (progressBarBoundStructureId == structureId)
                {
                    progressBar.StartCreation(creationTime);
                    progressBar.transform.localPosition = (1.1f * Vector3.up);
                }
            }

            float elapsed = 0f;
            while (elapsed < creationTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (progressBar != null)
            {
                if (progressBarBoundStructureId == structureId)
                    HideProgressBarVisual(resetProgress: true);
            }

            if (StructureManager.Instance == null)
            {
                Debug.LogError($"[InterfaceInstance] StructureManager.Instance est null -> spawn annulé. playerId={req.buildingPlayerId}, type={req.type}", this);
            }
            else
            {
                StructureInstance sourceStructure = StructureInstance.FindByInstanceId(req.sourceStructureId);
                StructureManager.Instance.SpawnUnitByTypeAtPosition(
                    req.buildingPlayerId,
                    req.type,
                    req.x,
                    req.z,
                    req.isPoweredUnit,
                    req.isProtector,
                    sourceStructure
                );
            }

            ShiftQueueLeft(req.sourceStructureId);
            creationState.hasCurrentRequest = false;
            creationState.currentCreationDuration = 0f;
        }

        creationState.coroutine = null;
        creationState.hasCurrentRequest = false;
        creationState.currentCreationDuration = 0f;
        if (progressBarBoundStructureId == structureId)
            HideProgressBarVisual(resetProgress: true);
    }

    private void ShiftQueueLeft(int structureId)
    {
        if (structureId == -1)
            return;

        var queue = GetOrCreateStructureQueue(structureId);
        if (queue.Count > 0)
            queue.RemoveAt(0);
        RefreshQueueSlotsVisibility();
    }

    private void SetAllQueueSlotsActive(bool active)
    {
        SetImageArrayActive(queueSlots, active);
    }

    private void RefreshQueueSlotsVisibility()
    {
        if (queueSlots == null) return;
        int structureId = GetCurrentStructureId();
        List<Sprite> queue = null;
        if (structureId != -1)
            queuedSpritesByStructure.TryGetValue(structureId, out queue);

        for (int i = 0; i < queueSlots.Length; i++)
        {
            var img = queueSlots[i];
            if (img == null) continue;
            Sprite sprite = (queue != null && i < queue.Count) ? queue[i] : null;
            img.sprite = sprite;
            bool hasSprite = sprite != null;
            img.gameObject.SetActive(hasSprite);
        }
    }

    private int GetCurrentStructureId()
    {
        if (displayedStructure == null)
            displayedStructure = StructureInstance.CurrentlySelected;
        return displayedStructure != null ? displayedStructure.GetInstanceID() : -1;
    }

    private List<Sprite> GetOrCreateStructureQueue(int structureId)
    {
        if (!queuedSpritesByStructure.TryGetValue(structureId, out var queue))
        {
            queue = new List<Sprite>();
            queuedSpritesByStructure[structureId] = queue;
        }
        return queue;
    }

    private BuildingCreationState GetOrCreateCreationState(int structureId)
    {
        if (!creationStateByStructure.TryGetValue(structureId, out var state))
        {
            state = new BuildingCreationState();
            creationStateByStructure[structureId] = state;
        }
        return state;
    }

    private void RefreshProgressBarVisibility()
    {
        if (progressBar == null)
            return;

        int displayedStructureId = GetCurrentStructureId();
        progressBarBoundStructureId = displayedStructureId;
        bool shouldShow = false;
        if (displayedStructureId != -1 && creationStateByStructure.TryGetValue(displayedStructureId, out var state) && state.hasCurrentRequest)
        {
            float elapsed = Time.time - state.currentCreationStartedAt;
            progressBar.StartCreationFromElapsed(state.currentCreationDuration, elapsed);
            shouldShow = true;
        }
        progressBar.SetFillVisible(shouldShow);
    }

    private void HideProgressBarVisual(bool resetProgress)
    {
        if (progressBar == null)
            return;

        if (resetProgress)
            progressBar.StopCreation(resetToZero: true);
        progressBar.SetFillVisible(false);
    }
}