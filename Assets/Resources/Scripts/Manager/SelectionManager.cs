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
    [SerializeField, Min(1f)] private float enemyUnitClickScreenRadius = 45f;

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
        {
            bool attackOrderIssued = TryIssueAttackMoveOrder();
            if (!attackOrderIssued)
                TryIssueGroupMoveOrder();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (TryIssueAttackMoveOrder())
            {
                isMouseDown = false;
                isDragging = false;
                SelectionBox.gameObject.SetActive(false);
                return;
            }

            isMouseDown = true;
            mouseStartPos = Mouse.current.position.ReadValue();
            foreach (SelectableObject so in CurrentlySelectedObjects)
            {
                so.DeselectMe();
            }
            CurrentlySelectedObjects.Clear();
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
            bool isLeader = (unit == leader);
            MovementManager.Instance.MoveBoatsUnitToPositionAsGroup(unit, hit.point, groupMoveStoppingDistance, groupMoveId, isLeader);
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

        if (TryIssueUnitAttackOrder())
            return true;

        return TryIssueStructureAttackOrder();
    }

    private bool TryIssueUnitAttackOrder()
    {
        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
        UnitInstance targetUnit = GetEnemyUnitUnderMouse(activePlayerId);
        if (targetUnit == null)
            return false;

        List<UnitInstance> groupUnits = CollectSelectedPlayerUnits(activePlayerId);
        if (!SelectionHasCombatUnit(groupUnits))
            return groupUnits.Count > 0;

        for (int i = 0; i < groupUnits.Count; i++)
        {
            UnitInstance unit = groupUnits[i];
            if (unit == null || unit.unitData == null)
                continue;

            float stopDistance = GetAttackStopDistance(unit);
            MovementManager.Instance.MoveBoatsUnitToTarget(unit, targetUnit.transform, stopDistance);
            Debug.Log($"[SelectionManager] Ordre d'attaque unite: {unit.name} -> {targetUnit.name}");
        }

        return true;
    }

    private bool TryIssueStructureAttackOrder()
    {
        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
        StructureInstance targetStructure = GetEnemyStructureUnderMouse(activePlayerId);
        if (targetStructure == null)
            return false;

        List<UnitInstance> groupUnits = CollectSelectedPlayerUnits(activePlayerId);
        if (!SelectionHasCombatUnit(groupUnits))
            return groupUnits.Count > 0;

        for (int i = 0; i < groupUnits.Count; i++)
        {
            UnitInstance unit = groupUnits[i];
            if (unit == null || unit.unitData == null)
                continue;

            float stopDistance = GetAttackStopDistance(unit);
            MovementManager.Instance.MoveBoatsUnitToTarget(unit, targetStructure.transform, stopDistance);
            Debug.Log($"[SelectionManager] Ordre d'attaque structure: {unit.name} -> {targetStructure.name}");
        }

        return true;
    }

    private UnitInstance GetEnemyUnitUnderMouse(int activePlayerId)
    {
        RaycastHit[] hits = GetMouseRaycastHits();
        UnitInstance bestUnit = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            UnitInstance unit = hitCollider.GetComponentInParent<UnitInstance>();
            if (unit == null)
                unit = hitCollider.GetComponent<UnitInstance>();
            if (unit == null)
                unit = hitCollider.GetComponentInChildren<UnitInstance>();

            if (unit == null || unit.playerId == activePlayerId)
                continue;

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestUnit = unit;
            }
        }

        if (bestUnit != null)
            return bestUnit;

        return GetEnemyUnitNearMouseOnScreen(activePlayerId);
    }

    private UnitInstance GetEnemyUnitNearMouseOnScreen(int activePlayerId)
    {
        if (AllSelectableObjects == null || Camera.main == null || Mouse.current == null)
            return null;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float bestDistanceSq = enemyUnitClickScreenRadius * enemyUnitClickScreenRadius;
        UnitInstance bestUnit = null;

        for (int i = AllSelectableObjects.Count - 1; i >= 0; i--)
        {
            SelectableObject selectable = AllSelectableObjects[i];
            if (selectable == null)
            {
                AllSelectableObjects.RemoveAt(i);
                continue;
            }

            UnitInstance unit = selectable.GetComponent<UnitInstance>();
            if (unit == null || unit.playerId == activePlayerId)
                continue;

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (screenPosition.z < 0f)
                continue;

            float distanceSq = ((Vector2)screenPosition - mousePosition).sqrMagnitude;
            if (distanceSq <= bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestUnit = unit;
            }
        }

        return bestUnit;
    }

    private StructureInstance GetEnemyStructureUnderMouse(int activePlayerId)
    {
        RaycastHit[] hits = GetMouseRaycastHits();
        StructureInstance bestStructure = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            StructureInstance structure = hitCollider.GetComponentInParent<StructureInstance>();
            if (structure == null)
                structure = hitCollider.GetComponent<StructureInstance>();
            if (structure == null)
                structure = hitCollider.GetComponentInChildren<StructureInstance>();

            if (structure == null || structure.playerId == activePlayerId)
                continue;

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestStructure = structure;
            }
        }

        return bestStructure;
    }

    private RaycastHit[] GetMouseRaycastHits()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.RaycastAll(ray);
    }

    private bool SelectionHasCombatUnit(List<UnitInstance> units)
    {
        if (units == null || units.Count == 0)
            return false;

        for (int i = 0; i < units.Count; i++)
        {
            UnitInstance unit = units[i];
            if (unit != null && unit.unitData is UnitCombatData)
                return true;
        }

        Debug.Log("Ordre refuse: uniquement des unites support/healer selectionnees.");
        return false;
    }

    private float GetAttackStopDistance(UnitInstance unit)
    {
        if (unit != null && unit.unitData is UnitCombatData combatData)
        {
            return Mathf.Max(0f, combatData.attackRange);
        }

        return 0.1f;
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

            MovementManager movement = unit.GetComponent<MovementManager>();
            if (movement == null)
                continue;

            units.Add(unit);
        }
        return units;
    }
}