using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TMPHoverUnderline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (textComponent != null)
        {
            textComponent.fontStyle |= FontStyles.Underline; 
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (textComponent != null)
        {
            textComponent.fontStyle &= ~FontStyles.Underline; 
        }
    }
}