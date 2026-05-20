using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class MainMenuManager : MonoBehaviour
{
    public void NewGame()  
    { 
        SceneManager.LoadScene("LaunchingLocalNewGame"); 
    }
    
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
    
    public void Options()  
    { 
        SceneManager.LoadScene("Options");   
    }
    
    public void Quit()     
    { 
        Application.Quit();                  
    }
}