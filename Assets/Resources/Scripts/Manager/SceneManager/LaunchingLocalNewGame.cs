using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

/// <summary>
/// Gère l'interface de configuration pour lancer une nouvelle partie locale (choix de la carte, nombre d'IA et difficulté).
/// </summary>
public class LaunchingLocalNewGameManager : MonoBehaviour
{
    [Header("Map Settings")]
    public Image mapImageDisplay;
    public TextMeshProUGUI mapNameDisplay;
    
    private string path;
    
    [Tooltip("Cette liste se met à jour automatiquement au lancement dans l'éditeur.")]
    public string[] mapNames;
    private int currentMapIndex = 0;

    [Header("AI Count Settings")]
    public TextMeshProUGUI aiCountDisplay;
    private int aiCount = 1;
    public int maxAi = 7;

    [Header("Difficulty Settings")]
    public TextMeshProUGUI diffDisplay;
    private string[] difficultiesEn = { "Easy", "Medium"};
    private string[] difficultiesFr = { "Facile", "Moyen"};
    private int currentDiffIndex = 0;

    void Start()
    {
        path = Path.Combine(Application.dataPath, "Resources", "Maps");
        
        LoadFoldersOnly();
        UpdateDisplays();
    }

    /// <summary>
    /// Récupère dynamiquement les noms des dossiers dans l'éditeur. 
    /// En Build, il conserve la dernière liste détectée sans écraser les données.
    /// </summary>
    void LoadFoldersOnly()
    {
        #if UNITY_EDITOR
        if (Directory.Exists(path))
        {
            string[] rawDirectories = Directory.GetDirectories(path);
            mapNames = new string[rawDirectories.Length];

            for (int i = 0; i < rawDirectories.Length; i++)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rawDirectories[i]);
                mapNames[i] = dirInfo.Name; // Stocke proprement "EUROPE", "LOL", "TEST", etc.
            }
            
            // Force Unity à sauvegarder la liste détectée dans la scène pour le build final
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[Succès Éditeur] {mapNames.Length} dossiers de cartes détectés de manière dynamique !");
        }
        else
        {
            Debug.LogError("Le dossier spécifié n'existe pas : " + path);
        }
        #else
        // En mode Build final (itch.io), le tableau 'mapNames' contiendra automatiquement 
        // les données détectées lors de ta dernière session dans l'éditeur. 
        // C'est 100% dynamique pour l'équipe de dev à chaque modification !
        Debug.Log($"[Build Runtime] Chargement de {mapNames.Length} cartes depuis l'index sauvegardé.");
        #endif
    }
    
    /// <summary>
    /// Passe à la carte suivante dans la liste. Boucle au début si la fin est atteinte.
    /// </summary>
    public void NextMap()
    {
        if (mapNames == null || mapNames.Length == 0) return;

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
        if (mapNames == null || mapNames.Length == 0) return;

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
        if (mapNames != null && mapNames.Length > 0)
        {
            string currentMapName = mapNames[currentMapIndex];
            mapNameDisplay.text = currentMapName;

            // Le chemin cible utilise désormais un nom de carte valide
            string resourcePath = "Maps/" + currentMapName + "/MapLayout";
            
            Texture2D loadedTexture = Resources.Load<Texture2D>(resourcePath);

            if (loadedTexture != null)
            {
                Sprite newSprite = Sprite.Create(
                    loadedTexture, 
                    new Rect(0, 0, loadedTexture.width, loadedTexture.height), 
                    new Vector2(0.5f, 0.5f)
                );
                
                mapImageDisplay.sprite = newSprite;
            }
            else
            {
                Debug.LogWarning($"Impossible de charger l'image à l'emplacement : Resources/{resourcePath}");
                mapImageDisplay.sprite = null;
            }
        }
        else
        {
            mapNameDisplay.text = "No Maps";
            mapImageDisplay.sprite = null;
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
    
    /// <summary>
    /// Valide les paramètres et charge la scène de jeu.
    /// </summary>
     public void StartGame()
     { 
         if (mapNames == null || mapNames.Length == 0) return;

         string selectedMap = mapNames[currentMapIndex];
         int selectedDifficulty = currentDiffIndex + 1;
         
         AutoLauncher.Request(selectedMap, 1, aiCount, selectedDifficulty);
         SceneManager.LoadScene("Game");
     }

    /// <summary>
    /// Annule la configuration et retourne au menu principal.
    /// </summary>
    public void Back()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}