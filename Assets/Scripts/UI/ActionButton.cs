using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButton : MonoBehaviour
{
    private Button button;
    private System.Action onClickAction;
    private Color originalColor;
    private Color pressedColor = new Color(0.1f, 0.7f, 2f, 1f);
    private float colorResetTimer = 0f;
    private bool isColorTemporarilyChanged = false;

    public void Setup(StructureType structureType,System.Action onClick)
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
		if (structureType == StructureType.NeutralStructure)
        {
            pressedColor = new Color(0.7f, 0.1f, 0.5f, 1f);
        }

        onClickAction = onClick;
        if (button.targetGraphic != null)
        {
            originalColor = button.targetGraphic.color;
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Execute);
    }

    void Update()
    {
        if (isColorTemporarilyChanged)
        {
            colorResetTimer -= Time.deltaTime;
            if (colorResetTimer <= 0f)
            {
                ResetColor();
                isColorTemporarilyChanged = false;
            }
        }
    }

    void Execute()
    {
        Debug.Log($"[{name}] Execute() appelé");
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = pressedColor;
            colorResetTimer = 0.07f;
            isColorTemporarilyChanged = true;
        }

        onClickAction?.Invoke();
    }

    public void ResetColor()
    {
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = originalColor;
        }
    }
}