using TMPro;
using UnityEngine;

public class TerritoryStructureName : MonoBehaviour
{
    [SerializeField] private TMP_Text territoryText;
    [SerializeField] private string territoryName;

    /// <summary>
    /// Récupère la référence du texte et rafraîchit l'affichage initial.
    /// </summary>
    private void Awake()
    {
        if (territoryText == null)
            territoryText = GetComponentInChildren<TMP_Text>(true);

        RefreshText();
    }

    /// <summary>
    /// Définit le nom du territoire affiché.
    /// </summary>
    public void SetTerritoryName(string newTerritoryName)
    {
        territoryName = newTerritoryName;
        RefreshText();
    }

    /// <summary>
    /// Met à jour la couleur du texte du territoire.
    /// </summary>
    public void SetColor(Color color)
    {
        if (territoryText == null)
            territoryText = GetComponentInChildren<TMP_Text>(true);

        if (territoryText != null)
            territoryText.color = color;
    }

    /// <summary>
    /// Réapplique le texte et la visibilité selon le nom de territoire.
    /// </summary>
    private void RefreshText()
    {
        if (territoryText == null)
            return;
        bool hasName = !string.IsNullOrWhiteSpace(territoryName);
        territoryText.text = territoryName;
        territoryText.gameObject.SetActive(hasName);
    }
}