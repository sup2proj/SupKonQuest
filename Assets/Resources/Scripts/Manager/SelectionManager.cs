using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    public RectTransform SelectionBox;
    public List<SelectableObject> AllSelectableObjects;
    public List<SelectableObject> CurrentlySelectedObjects;
    [SerializeField, Min(0.05f)] private float selectableRefreshInterval = 0.25f;
    [SerializeField, Min(0.01f)] private float groupMoveStoppingDistance = 0.1f;

    bool isMouseDown, isDragging = false;
    float selectableRefreshTimer;
    int nextGroupMoveId = 1;

    Vector3 mouseStartPos;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        AnalyzeSelectableObjectsContinuously();

        if (Mouse.current.rightButton.wasPressedThisFrame)
            TryIssueGroupMoveOrder();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool attackOrderIssued = TryIssueAttackMoveOrder();
            if (!attackOrderIssued)
            {
                isMouseDown = true;
                mouseStartPos = Mouse.current.position.ReadValue();
                foreach (SelectableObject so in CurrentlySelectedObjects)
                {
                    so.DeselectMe();
                }
                CurrentlySelectedObjects.Clear();
            }
        }
    
        if (isMouseDown)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();

            if (Vector3.Distance(mouseStartPos, currentMousePos) > 1 && !isDragging)
            {
                isDragging = true;
                SelectionBox.gameObject.SetActive(true);
            }
            if (isDragging)
            {
                float boxWidth = Mathf.Abs(currentMousePos.x - mouseStartPos.x);
                float boxHeight = Mathf.Abs(currentMousePos.y - mouseStartPos.y);

                SelectionBox.sizeDelta = new Vector2(boxWidth, boxHeight);
                SelectionBox.anchoredPosition = (mouseStartPos + currentMousePos) / 2;

                SelectUnits();
            }
        }
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isMouseDown = false;
            isDragging = false;
            SelectionBox.gameObject.SetActive(false);
        }
    }

    private void TryIssueGroupMoveOrder()
    {
        if (CurrentlySelectedObjects == null || CurrentlySelectedObjects.Count == 0 || Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
        List<UnitInstance> groupUnits = CollectSelectedPlayerUnits(activePlayerId);

        if (groupUnits.Count == 0)
            return;

        int groupMoveId = nextGroupMoveId++;
        UnitInstance leader = GetClosestUnitToPoint(groupUnits, hit.point);

        for (int i = 0; i < groupUnits.Count; i++)
        {
            UnitInstance unit = groupUnits[i];
            UnitsAnimation mover = unit.GetComponent<UnitsAnimation>();
            if (mover == null)
                continue;

            bool isLeader = (unit == leader);
            mover.MoveToPositionAsGroup(hit.point, groupMoveStoppingDistance, groupMoveId, isLeader);
        }
    }

    private UnitInstance GetClosestUnitToPoint(List<UnitInstance> units, Vector3 point)
    {
        UnitInstance closest = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < units.Count; i++)
        {
            UnitInstance unit = units[i];
            if (unit == null)
                continue;

            float distSq = (unit.transform.position - point).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                closest = unit;
            }
        }

        return closest;
    }
    
    private void AnalyzeSelectableObjectsContinuously()
    {
        selectableRefreshTimer += Time.deltaTime;
        if (selectableRefreshTimer < selectableRefreshInterval)
            return;

        selectableRefreshTimer = 0f;
        ForceRefreshSelectableObjects();
    }

    private void ForceRefreshSelectableObjects()
    {
        var found = FindObjectsByType<SelectableObject>(FindObjectsSortMode.None);

        AllSelectableObjects.Clear();
        AllSelectableObjects.AddRange(found);
    }

    public void RegisterSelectable(SelectableObject selectable)
    {
        if (selectable == null)
            return;

        if (!AllSelectableObjects.Contains(selectable))
            AllSelectableObjects.Add(selectable);
    }


    private void SelectUnits()
    {
        if (Camera.main == null)
            return;

        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();

        for (int i = AllSelectableObjects.Count - 1; i >= 0; i--)
        {
            SelectableObject so = AllSelectableObjects[i];
            if (so == null)
            {
                AllSelectableObjects.RemoveAt(i);
                continue;
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(so.transform.position);
            float boxLeft = SelectionBox.anchoredPosition.x - (SelectionBox.sizeDelta.x / 2);
            float boxRight = SelectionBox.anchoredPosition.x + (SelectionBox.sizeDelta.x / 2);
            float boxTop = SelectionBox.anchoredPosition.y + (SelectionBox.sizeDelta.y / 2);
            float boxBottom = SelectionBox.anchoredPosition.y - (SelectionBox.sizeDelta.y / 2);

            UnitInstance unit = so.GetComponent<UnitInstance>();
            bool isProtectorUnit = unit != null && unit.unitData != null && unit.unitData.isProtector;

            if (isProtectorUnit)
            {
                if (CurrentlySelectedObjects.Contains(so))
                {
                    CurrentlySelectedObjects.Remove(so);
                    so.DeselectMe();
                }
                continue;
            }

            if (screenPos.x > boxLeft && screenPos.x < boxRight && screenPos.y > boxBottom && screenPos.y < boxTop)
            {
                if (unit != null && unit.playerId != activePlayerId)
                    continue;

                if (!CurrentlySelectedObjects.Contains(so))
                {
                    CurrentlySelectedObjects.Add(so);
                    so.SelectMe();
                }
            }
            else
            {
                if (CurrentlySelectedObjects.Contains(so))
                {
                    CurrentlySelectedObjects.Remove(so);
                    so.DeselectMe();
                }

            }
        }
    }

    // Règle d'ordre d'attaque/déplacement :
    // - Si la sélection contient uniquement des Supports/Healers => on consomme le clic mais on ne déplace personne.
    // - Si la sélection contient au moins une unité de combat => on déplace TOUTES les unités valides (combat + support + healer)
    //   vers la cible ennemie. Les unités de combat s'arrêtent à leur attackRange, les autres suivent sans portée propre.
    private bool TryIssueAttackMoveOrder()
    {
        if (CurrentlySelectedObjects == null || CurrentlySelectedObjects.Count == 0 || Camera.main == null)
            return false;
    
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return false;

        UnitInstance targetUnit = hit.collider != null ? hit.collider.GetComponentInParent<UnitInstance>() : null;
        StructureInstance targetStructure = hit.collider != null ? hit.collider.GetComponentInParent<StructureInstance>() : null;
        if (targetUnit == null && targetStructure == null)
            return false;
    
        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();

        Transform targetTransform = null;
        bool isStructureTarget = false;

        if (targetUnit != null)
        {
            if (targetUnit.playerId == activePlayerId)
                return false;
            targetTransform = targetUnit.transform;
        }
        else
        {
            if (targetStructure.playerId == activePlayerId)
                return false;
            targetTransform = targetStructure.transform;
            isStructureTarget = true;
        }
    
        List<UnitInstance> groupUnits = CollectSelectedPlayerUnits(activePlayerId);
        bool hasCombatUnit = false;

        for (int i = 0; i < groupUnits.Count; i++)
        {
            UnitInstance unit = groupUnits[i];
            if (unit != null && unit.unitData is UnitCombatData)
                hasCombatUnit = true;
        }
    
        if (groupUnits.Count == 0)
            return false;
    
        if (!hasCombatUnit)
        {
            Debug.Log("Ordre refusé: uniquement des unités support/healer sélectionnées.");
            return true;
        }
    
        foreach (UnitInstance unit in groupUnits)
        {
            if (unit == null || unit.unitData == null)
                continue;

            UnitsAnimation mover = unit.GetComponent<UnitsAnimation>();
            if (mover == null)
                continue;

            float stopDistance = 0.1f;

            if (unit.unitData is UnitCombatData combatData)
            {
                stopDistance = Mathf.Max(0f, combatData.attackRange);
                if (isStructureTarget)
                {
                    UnitsType type = unit.unitData.type;
                    if (type == UnitsType.AntiBlindage || type == UnitsType.Heavy || type == UnitsType.Infantry)
                        stopDistance += 1f;
                }
            }

            mover.MoveToTarget(targetTransform, stopDistance);
        }
    
        return true;
    }

    private List<UnitInstance> CollectSelectedPlayerUnits(int activePlayerId)
    {
        List<UnitInstance> units = new List<UnitInstance>();

        if (CurrentlySelectedObjects == null)
            return units;

        for (int i = 0; i < CurrentlySelectedObjects.Count; i++)
        {
            SelectableObject so = CurrentlySelectedObjects[i];
            if (so == null)
                continue;

            UnitInstance unit = so.GetComponent<UnitInstance>();
            if (unit == null || unit.unitData == null || unit.playerId != activePlayerId || unit.unitData.isProtector)
                continue;

            UnitsAnimation mover = unit.GetComponent<UnitsAnimation>();
            if (mover == null)
                continue;

            units.Add(unit);
        }
        return units;
    }
}