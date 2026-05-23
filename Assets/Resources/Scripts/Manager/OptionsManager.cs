using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        ApplyMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        ApplySFXVolume(value);
    }

    void ApplyMusicVolume(float value)
    {
        if (value <= 0) audioMixer.SetFloat("MusicVolume", -80f);
        else audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 80f);
    }
 
    void ApplySFXVolume(float value)
    {
        if (value <= 0) audioMixer.SetFloat("SFXVolume", -80f);
        else audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 80f);
    }

    public void NextLanguage()
    {
        currentLanguage = currentLanguage + 1;
        if (currentLanguage > languages.Length - 1) currentLanguage = 0;
        SaveAndApplyLanguage();
    }

    public void PreviousLanguage()
    {
        currentLanguage = currentLanguage - 1;
        if (currentLanguage < 0) currentLanguage = languages.Length - 1;
        SaveAndApplyLanguage();
    }

    void SaveAndApplyLanguage()
    {
        PlayerPrefs.SetInt("Language", currentLanguage);
        UpdateLanguageText();
        RefreshTranslationsInScene();
    }

    void UpdateLanguageText()
    {
        languageText.text = languages[currentLanguage];
    }

    void RefreshTranslationsInScene()
    {
        LocalizedText[] allLocalizedTexts = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None); 
        for (int i = 0; i < allLocalizedTexts.Length; i++)
        {
            allLocalizedTexts[i].UpdateText();
        }
    }

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

    public void SetResolution(int resolutionIndex)
    {
        Resolution res = resolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", resolutionIndex);
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