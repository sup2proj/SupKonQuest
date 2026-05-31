using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class PanelInMiddle : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI winOrDefeatTextField;
    [SerializeField] private string defeatText;
    [SerializeField] private string winText;
    [SerializeField] private Color defeatTextColor;
    [SerializeField] private Color winTextColor;
    [SerializeField] private Button quitButton;

    public bool HasAssignedPanelReferences => background != null || winOrDefeatTextField != null;

    private void Awake()
    {
        Hide();
        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
    }

    /// <summary>
    /// Masque complètement le panneau.
    /// </summary>
    public void Hide()
    {
        SetPanelVisible(false);
    }

    /// <summary>
    /// Affiche le message de victoire pour le joueur gagnant.
    /// </summary>
    public void ShowVictory(int winnerPlayerId)
    {
        ShowMessage(FormatMessage(winText, winnerPlayerId, $"Victoire du joueur {winnerPlayerId}"), winTextColor);
    }

    /// <summary>
    /// Affiche le message de défaite pour le joueur concerné.
    /// </summary>
    public void ShowDefeat(int playerId)
    {
        ShowMessage(FormatMessage(defeatText, playerId, $"Defaite du joueur {playerId}"), defeatTextColor);
    }

    /// <summary>
    /// Affiche un message personnalisé avec la couleur fournie.
    /// </summary>
    private void ShowMessage(string message, Color textColor)
    {
        gameObject.SetActive(true);
        SetPanelVisible(true);

        if (winOrDefeatTextField == null)
            return;

        winOrDefeatTextField.text = message;
        winOrDefeatTextField.color = GetVisibleTextColor(textColor);
    }

    /// <summary>
    /// Active ou désactive les éléments visuels du panneau.
    /// </summary>
    private void SetPanelVisible(bool visible)
    {
        if (background != null)
            background.gameObject.SetActive(visible);

        if (winOrDefeatTextField != null)
            winOrDefeatTextField.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Formate un message en remplaçant le placeholder joueur si nécessaire.
    /// </summary>
    private string FormatMessage(string messageTemplate, int playerId, string fallback)
    {
        if (string.IsNullOrWhiteSpace(messageTemplate))
            return fallback;

        if (!messageTemplate.Contains("{0}"))
            return messageTemplate;

        return string.Format(messageTemplate, playerId);
    }

    /// <summary>
    /// Garantit que la couleur du texte reste visible.
    /// </summary>
    private Color GetVisibleTextColor(Color color)
    {
        if (color.a <= 0f)
            color.a = 1f;

        return color;
    }

    public void Quit()
    {
        if (InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.QuitGame();
        }
    }
}
