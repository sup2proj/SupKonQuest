using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    [Header("UI - Interface")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            statusText.text = "Connexion aux serveurs... Prêt !";
        }
        catch (System.Exception e)
        {
            statusText.text = "Erreur d'initialisation des serveurs.";
            Debug.LogError(e);
        }
    }

    public async void SignUp()
    {
        statusText.text = "Création du compte en cours...";
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);
            
            statusText.text = "Compte créé et connecté avec succès !";
            Debug.Log("Compte créé. ID Unique du joueur : " + AuthenticationService.Instance.PlayerId);
        }
        catch (AuthenticationException ex)
        {
            statusText.text = "Erreur de création : " + ex.Message;
            Debug.LogError(ex);
        }
        catch (RequestFailedException ex)
        {
            statusText.text = "Erreur réseau.";
            Debug.LogError(ex);
        }
    }

    public async void SignIn()
    {
        statusText.text = "Connexion en cours...";
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);
            
            statusText.text = "Connecté ! Bienvenue.";
            Debug.Log("Connexion réussie. ID Unique : " + AuthenticationService.Instance.PlayerId);
            
        }
        catch (AuthenticationException ex)
        {
            statusText.text = "Identifiants incorrects.";
            Debug.LogError(ex);
        }
        catch (RequestFailedException ex)
        {
            statusText.text = "Erreur réseau.";
            Debug.LogError(ex);
        }
    }
}