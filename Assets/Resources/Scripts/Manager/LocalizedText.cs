using UnityEngine;
using TMPro;

/// <summary>
/// Composant attaché à un élément TextMeshProUGUI pour gérer automatiquement sa traduction en plusieurs langues (Anglais, Français, Italien) selon les préférences du joueur.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Header("Traductions")]
    public string englishText;
    public string frenchText;
    public string italianoText;

    private TextMeshProUGUI textComponent;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    /// <summary>
    /// Récupère la langue actuellement sauvegardée dans les paramètres (PlayerPrefs) et applique le texte correspondant au composant TextMeshPro.
    /// </summary>
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
        else if (currentLang == 2)
        {
            textComponent.text = italianoText;
        }
    }

    /// <summary>
    /// Permet de modifier les textes traduits directement depuis un autre script puis met à jour l'affichage immédiatement.
    /// </summary>
    /// <param name="english">Le texte dynamique à afficher en anglais.</param>
    /// <param name="french">Le texte dynamique à afficher en français.</param>
    /// <param name="italiano">Le texte dynamique à afficher en italien.</param>
    public void SetDynamicTranslations(string english, string french, string  italiano)
    {
        englishText = english;
        frenchText = french;
        italianoText = italiano;
        
        UpdateText();
    }
}