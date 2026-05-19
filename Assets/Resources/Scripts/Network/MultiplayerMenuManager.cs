using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

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
    public void GoToCreateLobby()
    {
        LobbyRoomManager.IsHost = true;
        SceneManager.LoadScene("LobbyRoomScene");
    }

    public void GoToJoinLobby()
    {
        SceneManager.LoadScene("JoinLobbyScene"); 
    }

    public void BackToLogin()
    {
        SceneManager.LoadScene("LoginScene"); 
    }

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