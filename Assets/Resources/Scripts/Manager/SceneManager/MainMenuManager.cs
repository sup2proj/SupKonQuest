using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;

/// <summary>
/// Gère la navigation générale du menu principal et gère la redirection vers les différents modes de jeu ou les paramètres.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    /// <summary>
    /// Charge la scène de configuration pour lancer une nouvelle partie en mode local.
    /// </summary>
    public void NewGame()  
    { 
        SceneManager.LoadScene("LaunchingLocalNewGame"); 
    }
    
    /// <summary>
    /// Initialise les services Unity si nécessaire et vérifie si le joueur possède déjà une session active. 
    /// Le redirige vers le menu multijoueur s'il est déjà connecté, sinon vers l'écran de connexion.
    /// </summary>
    public async void Multi() 
    { 
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Joueur déjà connecté");
                SceneManager.LoadScene("MultiplayerScene");
            }
            else
            {
                Debug.Log("Aucune session trouvée");
                SceneManager.LoadScene("LoginScene");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de la vérification de session : " + e);
            SceneManager.LoadScene("LoginScene"); 
        }
    }
    
    /// <summary>
    /// Charge la scène des paramètres (langue, audio, etc.).
    /// </summary>
    public void Options()  
    { 
        SceneManager.LoadScene("Options");   
    }
    
    /// <summary>
    /// Ferme complètement l'application (important : cette action n'a pas d'effet dans l'éditeur Unity, uniquement dans le jeu compilé).
    /// </summary>
    public void Quit()     
    { 
        Application.Quit();                  
    }
}