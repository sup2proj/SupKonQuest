using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButton : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public Image icon;
    public Image cooldownMask;
    public TMP_Text costText;

    private System.Action onClickAction;
    private Color originalColor;
    private Color pressedColor = new Color(0f, 0f, 0.5f, 1f); // Bleu foncé

    public void Setup(Sprite iconSprite, int cost, System.Action onClick)
    {
        icon.sprite = iconSprite;
        costText.text = cost.ToString();

        onClickAction = onClick;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Execute);
    }

    void Execute()
    {
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = pressedColor;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}