using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Header("Traductions")]
    public string englishText;
    public string frenchText;

    private TextMeshProUGUI textComponent;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    public void UpdateText()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        int currentLang = PlayerPrefs.GetInt("Language", 0);

        if (currentLang == 0)
        {
            textComponent.text = englishText;
        }
        else if (currentLang == 1)
        {
            textComponent.text = frenchText;
        }
    }
}