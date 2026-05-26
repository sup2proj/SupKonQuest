using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using Unity.Services.Authentication;

/// <summary>
/// Gère le menu central du mode multijoueur. Permet de naviguer vers la création ou la recherche de salons, gère le matchmaking rapide, et affiche les alertes si le joueur vient d'être déconnecté d'une partie.
/// </summary>
public class MultiplayerMenuManager : MonoBehaviour
{
    [Header("Interface")]
    public LocalizedText statusText;

    void Start()
    {
        if (PlayerPrefs.HasKey("DisconnectReasonEN") && PlayerPrefs.HasKey("DisconnectReasonFR") && PlayerPrefs.HasKey("DisconnectReasonIT"))
        {
            string errEN = PlayerPrefs.GetString("DisconnectReasonEN");
            string errFR = PlayerPrefs.GetString("DisconnectReasonFR");
            string errIT = PlayerPrefs.GetString("DisconnectReasonIT");

            if (statusText != null)
            {
                string formattedEN = "<color=red>" + errEN + "</color>";
                string formattedFR = "<color=red>" + errFR + "</color>";
                string formattedIT = "<color=red>" + errIT + "</color>";
                statusText.SetDynamicTranslations(formattedEN, formattedFR, formattedIT);
            }
            
            PlayerPrefs.DeleteKey("DisconnectReasonEN");
            PlayerPrefs.DeleteKey("DisconnectReasonFR");
            PlayerPrefs.DeleteKey("DisconnectReasonIT");
        }
        else
        {
            if (statusText != null)
            {
                statusText.SetDynamicTranslations("Welcome to the multiplayer menu.", "Bienvenue dans le menu multijoueur.","Benvenuto nel menu multigiocatore.");
            }
        }
    }

    /// <summary>
    /// Configure le joueur actuel en tant qu'hôte (Host) et charge la scène de la salle d'attente pour qu'il puisse paramétrer son nouveau salon.
    /// </summary>
    public void GoToCreateLobby()
    {
        LobbyRoomManager.IsHost = true;
        SceneManager.LoadScene("LobbyRoomScene");
    }

    /// <summary>
    /// Charge la scène du navigateur de serveurs permettant au joueur de chercher et de rejoindre un salon manuellement.
    /// </summary>
    public void GoToJoinLobby()
    {
        SceneManager.LoadScene("JoinLobbyScene"); 
    }

    /// <summary>
    /// Retourne au menu principal du jeu sans se déconnecter des services Unity.
    /// </summary>
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); 
    }

    /// <summary>
    /// Déconnecte officiellement le joueur des services d'authentification d'Unity et le renvoie vers l'écran de connexion (Login).
    /// </summary>
    public void SignOutAndLeave()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("Déconnexion réussie.");
        }
        SceneManager.LoadScene("LoginScene"); 
    }

    /// <summary>
    /// Tente de rejoindre automatiquement et aléatoirement un salon public disposant de places libres. 
    /// Si aucun salon n'est disponible, le joueur devient automatiquement l'hôte et crée un nouveau salon.
    /// </summary>
    public async void QuickMatchmaking()
    {
        if (statusText != null) statusText.SetDynamicTranslations("Searching for a match...", "Recherche d'une partie en cours...","Ricerca di una partita in corso...");
        try
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Joueur");
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions
            {
                Player = new Unity.Services.Lobbies.Models.Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, myName) },
                        { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "False") }
                    }
                }
            };

            Lobby joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            Debug.Log("Partie trouvée ! Connexion en tant que Client.");
            if (statusText != null) statusText.SetDynamicTranslations("Match found!", "Partie trouvée !","Trovato !");
            LobbyRoomManager.IsHost = false;
            LobbyRoomManager.JoinedLobby = joinedLobby;
            SceneManager.LoadScene("LobbyRoomScene");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Aucune partie trouvée. Je suis l'Hôte ! (" + e.Message + ")");
            if (statusText != null) statusText.SetDynamicTranslations("No match found. Creating a lobby...", "Aucune partie. Création d'un salon...","Nessuna parte. Creazione di una chat room...");

            LobbyRoomManager.IsHost = true;
            SceneManager.LoadScene("LobbyRoomScene");
        }
    }
}