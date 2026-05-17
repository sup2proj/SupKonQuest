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

    public bool HasAssignedPanelReferences => background != null || winOrDefeatTextField != null;

    private void Awake()
    {
        Hide();
    }

    public void Hide()
    {
        SetPanelVisible(false);
    }

    public void ShowVictory(int winnerPlayerId)
    {
        ShowMessage(FormatMessage(winText, winnerPlayerId, $"Victoire du joueur {winnerPlayerId}"), winTextColor);
    }

    public void ShowDefeat(int playerId)
    {
        ShowMessage(FormatMessage(defeatText, playerId, $"Defaite du joueur {playerId}"), defeatTextColor);
    }

    private void ShowMessage(string message, Color textColor)
    {
        gameObject.SetActive(true);
        SetPanelVisible(true);

        if (winOrDefeatTextField == null)
            return;

        winOrDefeatTextField.text = message;
        winOrDefeatTextField.color = GetVisibleTextColor(textColor);
    }

    private void SetPanelVisible(bool visible)
    {
        if (background != null)
            background.gameObject.SetActive(visible);

        if (winOrDefeatTextField != null)
            winOrDefeatTextField.gameObject.SetActive(visible);
    }

    private string FormatMessage(string messageTemplate, int playerId, string fallback)
    {
        if (string.IsNullOrWhiteSpace(messageTemplate))
            return fallback;

        if (!messageTemplate.Contains("{0}"))
            return messageTemplate;

        return string.Format(messageTemplate, playerId);
    }

    private Color GetVisibleTextColor(Color color)
    {
        if (color.a <= 0f)
            color.a = 1f;

        return color;
    }
}
