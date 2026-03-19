using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [Header("Volume")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Language")]
    public TextMeshProUGUI languageText;
    private string[] languages = { "English", "Français" };
    private int currentLanguage = 0;

    [Header("Resolution")]
    public TextMeshProUGUI resolutionText;
    private Resolution[] resolutions;
    private int currentResolution = 0;

    [Header("Fullscreen")]
    public Toggle fullscreenToggle;

    void Start()
    {
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        currentLanguage = PlayerPrefs.GetInt("Language", 0);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        resolutions = Screen.resolutions;
        currentResolution = PlayerPrefs.GetInt("Resolution", resolutions.Length - 1);
        UpdateLanguageText();
        UpdateResolutionText();
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void NextLanguage()
    {
        currentLanguage = currentLanguage + 1;
        if (currentLanguage > languages.Length - 1)
            currentLanguage = 0;

        PlayerPrefs.SetInt("Language", currentLanguage);
        UpdateLanguageText();
    }

    public void PreviousLanguage()
    {
        currentLanguage = currentLanguage - 1;
        if (currentLanguage < 0)
            currentLanguage = languages.Length - 1;

        PlayerPrefs.SetInt("Language", currentLanguage);
        UpdateLanguageText();
    }

    void UpdateLanguageText()
    {
        languageText.text = languages[currentLanguage];
    }

    public void NextResolution()
    {
        currentResolution = currentResolution + 1;
        if (currentResolution > resolutions.Length - 1)
            currentResolution = 0;

        ApplyResolution();
    }

    public void PreviousResolution()
    {
        currentResolution = currentResolution - 1;
        if (currentResolution < 0)
            currentResolution = resolutions.Length - 1;

        ApplyResolution();
    }

    void ApplyResolution()
    {
        Resolution res = resolutions[currentResolution];
        Screen.SetResolution(res.width, res.height, fullscreenToggle.isOn);
        PlayerPrefs.SetInt("Resolution", currentResolution);
        UpdateResolutionText();
    }

    void UpdateResolutionText()
    {
        Resolution res = resolutions[currentResolution];
        resolutionText.text = res.width + " x " + res.height;
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void Back()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }
}
