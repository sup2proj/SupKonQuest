using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Gère l'écran des paramètres du jeu (Volume, Langue, Résolution, Plein Écran) et sauvegarde les préférences du joueur (PlayerPrefs).
/// </summary>
public class OptionsManager : MonoBehaviour
{
    [Header("Volume")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Language")]
    public TextMeshProUGUI languageText;
    public GameObject languageRow;
    private string[] languages = { "English", "Français", "Italiano" };
    private int currentLanguage = 0;

    [Header("Resolution")]
    public TMP_Dropdown resolutionDropdown; 
    private Resolution[] resolutions;

    [Header("Fullscreen")]
    public Toggle fullscreenToggle;

    void Start()
    {
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        currentLanguage = PlayerPrefs.GetInt("Language", 0);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        ApplyMusicVolume(musicSlider.value);
        ApplySFXVolume(sfxSlider.value);
        UpdateLanguageText();
        InitializeResolutionDropdown();

        EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
    }

    /// <summary>
    /// Sauvegarde et applique le nouveau volume de la musique lorsque le joueur déplace le curseur.
    /// </summary>
    /// <param name="value">La valeur du curseur (généralement entre 0.0001 et 1).</param>
    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        ApplyMusicVolume(value);
    }

    /// <summary>
    /// Sauvegarde et applique le nouveau volume des effets sonores (SFX) lorsque le joueur déplace le curseur.
    /// </summary>
    /// <param name="value">La valeur du curseur (généralement entre 0.0001 et 1).</param>
    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        ApplySFXVolume(value);
    }

    /// <summary>
    /// Convertit la valeur linéaire du curseur en valeur logarithmique (décibels) compatible avec l'AudioMixer pour la musique.
    /// </summary>
    /// <param name="value">La valeur brute du volume à appliquer (généralement issue du Slider, entre 0.0001 et 1).</param>
    void ApplyMusicVolume(float value)
    {
        if (value <= 0) audioMixer.SetFloat("MusicVolume", -80f);
        else audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 80f);
    }
 
    /// <summary>
    /// Convertit la valeur linéaire du curseur en valeur logarithmique (décibels) compatible avec l'AudioMixer pour les effets sonores.
    /// </summary>
    /// <param name="value">La valeur brute du volume à appliquer (généralement issue du Slider, entre 0.0001 et 1).</param>
    void ApplySFXVolume(float value)
    {
        if (value <= 0) audioMixer.SetFloat("SFXVolume", -80f);
        else audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 80f);
    }

    /// <summary>
    /// Passe à la langue suivante dans la liste, boucle au début si nécessaire, puis applique le changement.
    /// </summary>
    public void NextLanguage()
    {
        currentLanguage = currentLanguage + 1;
        if (currentLanguage > languages.Length - 1) currentLanguage = 0;
        SaveAndApplyLanguage();
    }

    /// <summary>
    /// Revient à la langue précédente dans la liste, boucle à la fin si nécessaire, puis applique le changement.
    /// </summary>
    public void PreviousLanguage()
    {
        currentLanguage = currentLanguage - 1;
        if (currentLanguage < 0) currentLanguage = languages.Length - 1;
        SaveAndApplyLanguage();
    }

    /// <summary>
    /// Sauvegarde la nouvelle langue dans les paramètres et déclenche le rafraîchissement des textes.
    /// </summary>
    void SaveAndApplyLanguage()
    {
        PlayerPrefs.SetInt("Language", currentLanguage);
        UpdateLanguageText();
        RefreshTranslationsInScene();
    }

    /// <summary>
    /// Met à jour le texte à l'écran pour afficher le nom de la langue actuellement sélectionnée dans les options.
    /// </summary>
    void UpdateLanguageText()
    {
        languageText.text = languages[currentLanguage];
    }

    /// <summary>
    /// Recherche tous les scripts LocalizedText présents dans la scène active et force leur mise à jour immédiate avec la nouvelle langue.
    /// </summary>
    void RefreshTranslationsInScene()
    {
        LocalizedText[] allLocalizedTexts = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None); 
        for (int i = 0; i < allLocalizedTexts.Length; i++)
        {
            allLocalizedTexts[i].UpdateText();
        }
    }

    /// <summary>
    /// Récupère toutes les résolutions d'écran supportées par l'ordinateur du joueur, remplit le menu déroulant et sélectionne la résolution actuelle.
    /// </summary>
    void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (PlayerPrefs.HasKey("Resolution"))
            {
                if (i == PlayerPrefs.GetInt("Resolution")) currentResolutionIndex = i;
            }
            else if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    /// <summary>
    /// Applique la résolution choisie par le joueur dans le menu déroulant et sauvegarde son index.
    /// </summary>
    /// <param name="resolutionIndex">L'index de la résolution sélectionnée dans la liste du menu déroulant.</param>
    public void SetResolution(int resolutionIndex)
    {
        Resolution res = resolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", resolutionIndex);
    }

    /// <summary>
    /// Active ou désactive le mode plein écran selon l'état de la case à cocher, et sauvegarde ce choix.
    /// </summary>
    /// <param name="isFullscreen">Vrai pour activer le plein écran, faux pour passer en mode fenêtré.</param>
    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    /// <summary>
    /// Force l'écriture des sauvegardes sur le disque par sécurité et retourne à la scène du menu principal.
    /// </summary>
    public void Back()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }
}