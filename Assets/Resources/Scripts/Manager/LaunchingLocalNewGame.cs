using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère l'interface de configuration pour lancer une nouvelle partie locale (choix de la carte, nombre d'IA et difficulté).
/// </summary>
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
    private string[] difficultiesIT = { "Facile", "Medio", "Difficile" };
    private int currentDiffIndex = 0;

    void Start()
    {
        UpdateDisplays();
    }

    /// <summary>
    /// Passe à la carte suivante dans la liste. Boucle au début si la fin est atteinte.
    /// </summary>
    public void NextMap()
    {
        currentMapIndex++;
        if (currentMapIndex > mapNames.Length - 1)
        {
            currentMapIndex = 0;
        }
        UpdateDisplays();
    }
    
    /// <summary>
    /// Revient à la carte précédente dans la liste. Boucle à la fin si le début est atteint.
    /// </summary>
    public void PreviousMap()
    {
        currentMapIndex--;
        if (currentMapIndex < 0)
        {
            currentMapIndex = mapNames.Length - 1;
        }
        UpdateDisplays();
    }
    
    /// <summary>
    /// Augmente le nombre d'IA dans la partie, sans dépasser le maximum autorisé (maxAi).
    /// </summary>
    public void IncreaseAi()
    {
        aiCount++;
        if (aiCount > maxAi)
        {
            aiCount = maxAi;
        }
        UpdateDisplays();
    }
    
    /// <summary>
    /// Diminue le nombre d'IA dans la partie, sans descendre en dessous de zéro.
    /// </summary>
    public void DecreaseAi()
    {
        aiCount--;
        if (aiCount < 0)
        {
            aiCount = 0;
        }
        UpdateDisplays();
    }
    
    /// <summary>
    /// Passe au niveau de difficulté supérieur. Boucle au niveau le plus facile si la fin est atteinte.
    /// </summary>
    public void NextDifficulty()
    {
        currentDiffIndex++;
        
        if (currentDiffIndex > difficultiesEn.Length - 1)
        {
            currentDiffIndex = 0;
        }
        
        UpdateDisplays();
    }
    
    /// <summary>
    /// Revient au niveau de difficulté inférieur. Boucle au niveau le plus difficile si le début est atteint.
    /// </summary>
    public void PreviousDifficulty()
    {
        currentDiffIndex--;
        
        if (currentDiffIndex < 0)
        {
            currentDiffIndex = difficultiesEn.Length - 1;
        }
        
        UpdateDisplays();
    }

    /// <summary>
    /// Met à jour tous les éléments visuels de l'interface (textes, images) en fonction des paramètres actuels et de la langue sauvegardée par le joueur.
    /// </summary>
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
        else if (currentLang == 2)
        {
            diffDisplay.text = difficultiesIT[currentDiffIndex];
        }
    }

    /// <summary>
    /// Valide les paramètres et charge la scène de jeu.
    /// </summary>
    public void StartGame()
    {
        SceneManager.LoadScene("MainMenu"); 
    }

    /// <summary>
    /// Annule la configuration et retourne au menu principal.
    /// </summary>
    public void Back()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}