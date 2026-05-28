using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TMPHoverUnderline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI textComponent;

    /// <summary>
    /// Récupère le composant TextMeshProUGUI enfant utilisé pour appliquer le soulignement au survol.
    /// </summary>
    private void Awake()
    {
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Ajoute le style souligné au texte lorsque le pointeur entre dans la zone.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (textComponent != null)
        {
            textComponent.fontStyle |= FontStyles.Underline; 
        }
    }

    /// <summary>
    /// Retire le style souligné du texte lorsque le pointeur quitte la zone.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (textComponent != null)
        {
            textComponent.fontStyle &= ~FontStyles.Underline; 
        }
    }
}