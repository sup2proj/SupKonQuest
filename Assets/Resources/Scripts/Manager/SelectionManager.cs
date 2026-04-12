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

    bool isMouseDown, isDragging = false;
    float selectableRefreshTimer;

    Vector3 mouseStartPos;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        AnalyzeSelectableObjectsContinuously();

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

                selectUnits();
            }
        }
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isMouseDown = false;
            isDragging = false;
            SelectionBox.gameObject.SetActive(false);
        }
    }

    public void RegisterSelectable(SelectableObject selectable)
    {
        if (selectable == null)
            return;

        if (!AllSelectableObjects.Contains(selectable))
            AllSelectableObjects.Add(selectable);
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
        var found = FindObjectsOfType<SelectableObject>();

        AllSelectableObjects.Clear();
        AllSelectableObjects.AddRange(found);
    }

    void selectUnits()
    {
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

            if (screenPos.x > boxLeft && screenPos.x < boxRight && screenPos.y > boxBottom && screenPos.y < boxTop)
            {
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

private bool TryIssueAttackMoveOrder()
   {
       if (CurrentlySelectedObjects == null || CurrentlySelectedObjects.Count == 0)
           return false;
   
       Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
       if (!Physics.Raycast(ray, out RaycastHit hit))
           return false;
   
       UnitInstance targetUnit = hit.collider != null ? hit.collider.GetComponentInParent<UnitInstance>() : null;
       if (targetUnit == null)
           return false;
   
       int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
       if (targetUnit.playerId == activePlayerId)
           return false;
   
       List<UnitInstance> combatUnits = new List<UnitInstance>();
   
       for (int i = CurrentlySelectedObjects.Count - 1; i >= 0; i--)
       {
           SelectableObject so = CurrentlySelectedObjects[i];
           if (so == null)
               continue;
   
           UnitInstance unit = so.GetComponent<UnitInstance>();
           if (unit == null || unit.unitData == null)
               continue;
   
           if (unit.playerId != activePlayerId)
               continue;
   
           // On n'envoie pas les supports/healers
           UnitsType type = unit.unitData.type;
           if (type == UnitsType.Support || type == UnitsType.Healer)
               continue;
   
           combatUnits.Add(unit);
       }
   
       // Si aucune unité de combat, on considère que l'ordre est consommé
       // pour ne pas désélectionner la box au clic ennemi.
       if (combatUnits.Count == 0)
       {
           Debug.Log("Ordre refusé: aucune unité de combat sélectionnée.");
           return true;
       }
   
       foreach (UnitInstance unit in combatUnits)
       {
           UnitsAnimation mover = unit.GetComponent<UnitsAnimation>();
           if (mover == null)
               continue;
   
           float attackRange = 0f;
           if (unit.unitData is UnitCombatData combatData)
               attackRange = Mathf.Max(0f, combatData.attackRange);
   
           mover.MoveToTarget(targetUnit.transform, attackRange);
       }
   
       return true;
   }
   
}
