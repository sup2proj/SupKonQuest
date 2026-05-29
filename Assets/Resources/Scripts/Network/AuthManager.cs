using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère l'interface de connexion et d'inscription du joueur en utilisant les services d'authentification d'Unity (Unity Services).
/// </summary>
public class AuthManager : MonoBehaviour
{
    [Header("UI - Interface")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public LocalizedText statusText;

    async void Start()
    {
        usernameInput.Select();
        try
        {
            await UnityServices.InitializeAsync();
            if (statusText != null) statusText.SetDynamicTranslations("Connecting to servers... Ready!", "Connexion aux serveurs... Prêt !", "Connessione ai server... Pronti!");
        }
        catch (System.Exception e)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Server initialization error.", "Erreur d'initialisation des serveurs.", "Errore durante l'inizializzazione dei server.");
            Debug.LogError(e);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (usernameInput.isFocused)
            {
                passwordInput.Select();
            }
            else if (passwordInput.isFocused)
            {
                usernameInput.Select();
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SignIn();
        }
    }

    /// <summary>
    /// Tente de créer un nouveau compte joueur sur les serveurs Unity avec le nom d'utilisateur et le mot de passe saisis.
    /// En cas de succès, sauvegarde le pseudo localement et charge la scène multijoueur.
    /// </summary>
    public async void SignUp()
    {
        if (string.IsNullOrWhiteSpace(usernameInput.text) || string.IsNullOrWhiteSpace(passwordInput.text))
        {
            if (statusText != null) statusText.SetDynamicTranslations("Please fill all fields.", "Veuillez remplir tous les champs.", "Si prega di compilare tutti i campi.");
            return;
        }

        if (statusText != null) statusText.SetDynamicTranslations("Creating account...", "Création du compte en cours...", "Creazione dell'account in corso...");
            
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await AuthenticationService.Instance.AddUsernamePasswordAsync(usernameInput.text, passwordInput.text);

            PlayerPrefs.SetString("PlayerName", usernameInput.text);
            
            if (statusText != null) statusText.SetDynamicTranslations("Account created and connected successfully!", "Compte créé et connecté avec succès !", "Account creato e accesso effettuato con successo!");
                
            SceneManager.LoadScene("MultiplayerScene");
            Debug.Log("Compte créé. ID Unique du joueur : " + AuthenticationService.Instance.PlayerId);
        }
        catch (AuthenticationException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Creation error: " + ex.Message, "Erreur de création : " + ex.Message, "Errore durante la creazione : " + ex.Message);
            Debug.LogError(ex);
        }
        catch (RequestFailedException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Network error.", "Erreur réseau.", "Errore di rete.");
            Debug.LogError(ex);
        }
    }

    /// <summary>
    /// Tente de connecter un compte joueur existant sur les serveurs Unity avec les identifiants saisis.
    /// En cas de succès, met à jour le pseudo localement et charge la scène multijoueur.
    /// </summary>
    public async void SignIn()
    {
        if (string.IsNullOrWhiteSpace(usernameInput.text) || string.IsNullOrWhiteSpace(passwordInput.text))
        {
            if (statusText != null) statusText.SetDynamicTranslations("Invalid credentials.", "Identifiants incorrects.", "Dati di accesso non corretti.");
            return;
        }

        if (statusText != null) statusText.SetDynamicTranslations("Signing in...", "Connexion en cours...", "Accesso in corso...");
            
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);

            PlayerPrefs.SetString("PlayerName", usernameInput.text);
            
            if (statusText != null) statusText.SetDynamicTranslations("Connected! Welcome.", "Connecté ! Bienvenue.", "Hai effettuato l'accesso ! Benvenuto.");
                
            SceneManager.LoadScene("MultiplayerScene");
            Debug.Log("Connexion réussie. ID Unique : " + AuthenticationService.Instance.PlayerId);
        }
        catch (AuthenticationException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Invalid credentials.", "Identifiants incorrects.", "Dati di accesso non corretti.");
            Debug.LogError(ex);
        }
        catch (RequestFailedException ex)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Network error.", "Erreur réseau.", "Errore di rete.");
            Debug.LogError(ex);
        }
    }

    /// <summary>
    /// Annule le processus de connexion et retourne au menu principal du jeu.
    /// </summary>
    public void Back()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}