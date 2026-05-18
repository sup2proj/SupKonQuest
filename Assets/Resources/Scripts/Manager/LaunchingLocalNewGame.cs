using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LaunchingLocalNewGameManager : MonoBehaviour
{
    [Header("Map Settings")]
    public Image mapImageDisplay;
    public TextMeshProUGUI mapNameDisplay;
    public Sprite[] mapImages;
    public string[] mapNames;
    private int currentMapIndex = 0;

    [Header("AI Count Settings")]
    public TextMeshProUGUI aiCountDisplay;
    private int aiCount = 1;
    public int maxAi = 7;

    [Header("Difficulty Settings")]
    public TextMeshProUGUI diffDisplay;
    private string[] difficultiesEn = { "Easy", "Medium", "Hard" };
    private string[] difficultiesFr = { "Facile", "Moyen", "Difficile" };
    private int currentDiffIndex = 0;

    void Start()
    {
        UpdateDisplays();
    }

    public void NextMap()
    {
        currentMapIndex++;
        if (currentMapIndex > mapNames.Length - 1)
        {
            currentMapIndex = 0;
        }
        UpdateDisplays();
    }
    
    public void PreviousMap()
    {
        currentMapIndex--;
        if (currentMapIndex < 0)
        {
            currentMapIndex = mapNames.Length - 1;
        }
        UpdateDisplays();
    }
    
    public void IncreaseAi()
    {
        aiCount++;
        if (aiCount > maxAi)
        {
            aiCount = maxAi;
        }
        UpdateDisplays();
    }
    
    public void DecreaseAi()
    {
        aiCount--;
        if (aiCount < 0)
        {
            aiCount = 0;
        }
        UpdateDisplays();
    }
    
    public void NextDifficulty()
    {
        currentDiffIndex++;
        
        if (currentDiffIndex > difficultiesEn.Length - 1)
        {
            currentDiffIndex = 0;
        }
        
        UpdateDisplays();
    }
    
    public void PreviousDifficulty()
    {
        currentDiffIndex--;
        
        if (currentDiffIndex < 0)
        {
            currentDiffIndex = difficultiesEn.Length - 1;
        }
        
        UpdateDisplays();
    }

    void UpdateDisplays()
    {
        if (mapNames.Length > 0 && mapImages.Length > 0)
        {
            mapNameDisplay.text = mapNames[currentMapIndex];
            mapImageDisplay.sprite = mapImages[currentMapIndex];
        }
        
        aiCountDisplay.text = aiCount.ToString();
        
        int currentLang = PlayerPrefs.GetInt("Language", 0);
        
        if (currentLang == 0)
        {
            diffDisplay.text = difficultiesEn[currentDiffIndex];
        }
        else if (currentLang == 1)
        {
            diffDisplay.text = difficultiesFr[currentDiffIndex];
        }
    }

     public void StartGame()
     { 
         string selectedMap = "TEST";

         AutoLauncher.Request(selectedMap);
         SceneManager.LoadScene("Game");
     }

    public void Back()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}