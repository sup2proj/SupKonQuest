using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class SelectionManager : MonoBehaviour
{
    public RectTransform SelectionBox;
    public List<SelectableObject> AllSelectableObjects;
    public List<SelectableObject> CurrentlySelectedObjects;
    bool isMouseDown, isDragging;

    Vector3 mouseStartPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isDragging = false;
        isMouseDown = false;
        AllSelectableObjects = new List<SelectableObject>(FindObjectsOfType<SelectableObject>());

        Debug.Log("Nombre d'unités trouvées : " + AllSelectableObjects.Count);
    }

    // Update is called once per frame
    void Update()
    {
        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
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

    void selectUnits()
    {
        Debug.Log("test select unit");
        foreach (SelectableObject so in AllSelectableObjects)
        {
            Debug.Log("test foreach");
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
}
