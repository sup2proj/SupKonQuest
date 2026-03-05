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

    public void Setup(System.Action onClick)
    {
        Debug.Log($"[{name}] Setup() appelé");
        
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            Debug.LogError($"[{name}] ActionButton: aucun Button trouvé/assigné.", this);
            return;
        }

        Debug.Log($"[{name}] Button trouvé: {button != null}");
        
        onClickAction = onClick;
        if (button.targetGraphic != null)
        {
            originalColor = button.targetGraphic.color;
            Debug.Log($"[{name}] Couleur originale sauvegardée: {originalColor}");
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Execute);
        
        Debug.Log($"[{name}] Setup terminé - Listener ajouté");
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