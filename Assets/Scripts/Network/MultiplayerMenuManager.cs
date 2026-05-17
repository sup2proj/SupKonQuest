using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

public class MultiplayerMenuManager : MonoBehaviour
{
    [Header("Interface")]
    public TextMeshProUGUI statusText;

    void Start()
    {
        if (PlayerPrefs.HasKey("DisconnectReason"))
        {
            string errorMessage = PlayerPrefs.GetString("DisconnectReason");
            
            if (statusText != null)
            {
                statusText.text = "<color=red>" + errorMessage + "</color>";
            }
            PlayerPrefs.DeleteKey("DisconnectReason");
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "Bienvenue dans le menu multijoueur.";
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
        if (statusText != null) statusText.text = "Recherche d'une partie en cours...";
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
            if (statusText != null) statusText.text = "Partie trouvée !";
            LobbyRoomManager.IsHost = false;
            LobbyRoomManager.JoinedLobby = joinedLobby;
            SceneManager.LoadScene("LobbyRoomScene");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Aucune partie trouvée. Je suis l'Hôte ! (" + e.Message + ")");
            if (statusText != null) statusText.text = "Aucune partie. Création d'un salon...";

            LobbyRoomManager.IsHost = true;
            SceneManager.LoadScene("LobbyRoomScene");
        }
    }
}