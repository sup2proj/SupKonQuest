using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    [Header("UI - Interface")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public LocalizedText statusText;

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (statusText != null) statusText.SetDynamicTranslations("Connecting to servers... Ready!", "Connexion aux serveurs... Prêt !");
        }
        catch (System.Exception e)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Server initialization error.", "Erreur d'initialisation des serveurs.");
            Debug.LogError(e);
        }
    }

    public async void SignUp()
    {
        if (statusText != null) statusText.SetDynamicTranslations("Creating account...", "Création du compte en cours...");
            
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);

            PlayerPrefs.SetString("PlayerName", usernameInput.text);
            
            if (statusText != null) statusText.SetDynamicTranslations("Account created and connected successfully!", "Compte créé et connecté avec succès !");
                
            SceneManager.LoadScene("MultiplayerScene");
            Debug.Log("Compte créé. ID Unique du joueur : " + AuthenticationService.Instance.PlayerId);
        }
        catch (AuthenticationException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Creation error: " + ex.Message, "Erreur de création : " + ex.Message);
            Debug.LogError(ex);
        }
        catch (RequestFailedException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Network error.", "Erreur réseau.");
            Debug.LogError(ex);
        }
    }

    public async void SignIn()
    {
        if (statusText != null) statusText.SetDynamicTranslations("Signing in...", "Connexion en cours...");
            
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);

            PlayerPrefs.SetString("PlayerName", usernameInput.text);
            
            if (statusText != null) statusText.SetDynamicTranslations("Connected! Welcome.", "Connecté ! Bienvenue.");
                
            SceneManager.LoadScene("MultiplayerScene");
            Debug.Log("Connexion réussie. ID Unique : " + AuthenticationService.Instance.PlayerId);
            
        }
        catch (AuthenticationException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Invalid credentials.", "Identifiants incorrects.");
            Debug.LogError(ex);
        }
        catch (RequestFailedException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Network error.", "Erreur réseau.");
            Debug.LogError(ex);
        }
    }

    public void Back()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}