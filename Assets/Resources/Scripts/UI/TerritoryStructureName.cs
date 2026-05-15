using TMPro;
using UnityEngine;

public class TerritoryStructureName : MonoBehaviour
{
    [SerializeField] private TMP_Text territoryText;
    [SerializeField] private string territoryName;

    private void Awake()
    {
        if (territoryText == null)
            territoryText = GetComponentInChildren<TMP_Text>(true);

        RefreshText();
    }

    public void SetTerritoryName(string newTerritoryName)
    {
        territoryName = newTerritoryName;
        RefreshText();
    }

    public void SetColor(Color color)
    {
        if (territoryText == null)
            territoryText = GetComponentInChildren<TMP_Text>(true);

        if (territoryText != null)
            territoryText.color = color;
    }

    private void RefreshText()
    {
        if (territoryText == null)
            return;
        bool hasName = !string.IsNullOrWhiteSpace(territoryName);
        territoryText.text = territoryName;
        territoryText.gameObject.SetActive(hasName);
    }
}