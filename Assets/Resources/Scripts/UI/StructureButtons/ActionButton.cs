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

    /// <summary>
    /// Configure le bouton d'action : définit le type de structure, la couleur pressée selon le type,
    /// enregistre l'action à exécuter lors du clic et attache le listener Unity.
    /// </summary>
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

    /// <summary>
    /// Met à jour le timer de réinitialisation de couleur lorsqu'une couleur temporaire a été appliquée.
    /// </summary>
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

    /// <summary>
    /// Exécute l'action liée au bouton : applique temporairement la couleur "pressée" puis appelle l'action.
    /// </summary>
    void Execute()
    {
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = pressedColor;
            colorResetTimer = 0.07f;
            isColorTemporarilyChanged = true;
        }

        onClickAction?.Invoke();
    }

    /// <summary>
    /// Réinitialise la couleur du graphique cible du bouton à sa couleur d'origine.
    /// </summary>
    public void ResetColor()
    {
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = originalColor;
        }
    }
}