using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LaunchingLocalNewGameManager : MonoBehaviour
{
    [Header("Menu Navigation")]
    public TextMeshProUGUI[] rowTitles; 
    public float normalSize = 40f;
    public float selectedSize = 50f;
    private int currentIndex = 0;

    [Header("Map Settings")]
    public Image mapImageDisplay;
    public TextMeshProUGUI mapNameDisplay;
    public TextMeshProUGUI mapLeftArrow;
    public TextMeshProUGUI mapRightArrow;
    public Sprite[] mapImages;
    public string[] mapNames;
    private int currentMapIndex = 0;

    [Header("AI Count Settings")]
    public TextMeshProUGUI aiCountDisplay;
    public TextMeshProUGUI aiLeftArrow;
    public TextMeshProUGUI aiRightArrow;
    private int aiCount = 1;
    public int maxAi = 7;

    [Header("Difficulty Settings")]
    public TextMeshProUGUI diffDisplay;
    public TextMeshProUGUI diffLeftArrow;
    public TextMeshProUGUI diffRightArrow;
    private string[] difficultiesEn = { "Easy", "Medium", "Hard" };
    private string[] difficultiesFr = { "Facile", "Moyen", "Difficile" };
    private int currentDiffIndex = 0;

    [Header("Arrow Colors")]
    public Color normalArrowColor = Color.grey;
    public Color selectedArrowColor = Color.white;

    void Start()
    {
        UpdateDisplays();
        UpdateVisuals();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentIndex == 0)
            {
                currentIndex = 1;
            }
            else if (currentIndex == 1)
            {
                currentIndex = 2;
            }
            else if (currentIndex == 2)
            {
                currentIndex = 3;
            }
            else if (currentIndex == 3 || currentIndex == 4)
            {
                currentIndex = 0;
            }
            UpdateVisuals();
        }
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentIndex == 0)
            {
                currentIndex = 3;
            }
            else if (currentIndex == 1)
            {
                currentIndex = 0;
            }
            else if (currentIndex == 2)
            {
                currentIndex = 1;
            }
            else if (currentIndex == 3 || currentIndex == 4)
            {
                currentIndex = 2;
            }
            UpdateVisuals();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentIndex == 0)
            {
                NextMap();
            }
            else if (currentIndex == 1)
            {
                IncreaseAi();
            }
            else if (currentIndex == 2)
            {
                NextDifficulty();
            }
            else if (currentIndex == 3)
            {
                currentIndex = 4;
                UpdateVisuals();
            }
            else if (currentIndex == 4)
            {
                currentIndex = 3;
                UpdateVisuals();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentIndex == 0)
            {
                PreviousMap();
            }
            else if (currentIndex == 1)
            {
                DecreaseAi();
            }
            else if (currentIndex == 2)
            {
                PreviousDifficulty();
            }
            else if (currentIndex == 3)
            {
                currentIndex = 4;
                UpdateVisuals();
            }
            else if (currentIndex == 4)
            {
                currentIndex = 3;
                UpdateVisuals();
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (currentIndex == 3)
            {
                StartGame();
            }
            else if (currentIndex == 4)
            {
                Back();
            }
        }
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < rowTitles.Length; i++)
        {
            if (i == currentIndex)
            {
                rowTitles[i].fontSize = selectedSize;
            }
            else
            {
                rowTitles[i].fontSize = normalSize;
            }
        }

        mapLeftArrow.color = normalArrowColor; 
        mapRightArrow.color = normalArrowColor;
        
        aiLeftArrow.color = normalArrowColor; 
        aiRightArrow.color = normalArrowColor;
        
        diffLeftArrow.color = normalArrowColor; 
        diffRightArrow.color = normalArrowColor;

        if (currentIndex == 0)
        {
            mapLeftArrow.color = selectedArrowColor; 
            mapRightArrow.color = selectedArrowColor;
        }
        else if (currentIndex == 1)
        {
            aiLeftArrow.color = selectedArrowColor; 
            aiRightArrow.color = selectedArrowColor;
        }
        else if (currentIndex == 2)
        {
            diffLeftArrow.color = selectedArrowColor; 
            diffRightArrow.color = selectedArrowColor;
        }
    }

    void NextMap()
    {
        currentMapIndex++;
        if (currentMapIndex > mapNames.Length - 1)
        {
            currentMapIndex = 0;
        }
        UpdateDisplays();
    }
    
    void PreviousMap()
    {
        currentMapIndex--;
        if (currentMapIndex < 0)
        {
            currentMapIndex = mapNames.Length - 1;
        }
        UpdateDisplays();
    }
    
    void IncreaseAi()
    {
        aiCount++;
        if (aiCount > maxAi)
        {
            aiCount = maxAi;
        }
        UpdateDisplays();
    }
    
    void DecreaseAi()
    {
        aiCount--;
        if (aiCount < 0)
        {
            aiCount = 0;
        }
        UpdateDisplays();
    }
    
    void NextDifficulty()
    {
        currentDiffIndex++;
        
        if (currentDiffIndex > difficultiesEn.Length - 1)
        {
            currentDiffIndex = 0;
        }
        
        UpdateDisplays();
    }
    
    void PreviousDifficulty()
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

    void StartGame()
    {
        SceneManager.LoadScene("MainMenu"); 
    }

    void Back()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}