using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HostLobbyManager : MonoBehaviour
{
    private Lobby hostLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    [Header("Interface (UI)")]
    public TextMeshProUGUI lobbyStatusText;
    public TextMeshProUGUI playerListText;
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI maxPlayersText;

    private string currentMap = "Europe";
    private int currentMaxPlayers = 4;

    async void Start()
    {
        lobbyStatusText.text = "Création du Lobby en cours...";
        await CreateLobby();
    }

    void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    private async System.Threading.Tasks.Task CreateLobby()
    {
        try
        {
            string lobbyName = "Salon de " + AuthenticationService.Instance.PlayerId;
            
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, currentMap) }
                }
            };

            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, currentMaxPlayers, options);
            
            RefreshUI();
            lobbyStatusText.text = "Lobby Ouvert ! En attente de joueurs...";
            Debug.Log("Lobby créé avec succès ! ID: " + hostLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            lobbyStatusText.text = "Erreur de création.";
            Debug.LogError(e);
        }
    }


    public async void ChangeMap(string newMap)
    {
        if (hostLobby == null) return;

        try
        {
            currentMap = newMap;
            
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, currentMap) }
                }
            };
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, options);
            RefreshUI();
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public async void ChangeMaxPlayers(int newMax)
    {
        if (hostLobby == null) return;

        try
        {
            currentMaxPlayers = newMax;
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                MaxPlayers = currentMaxPlayers
            };
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, options);
            RefreshUI();
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public async void LeaveLobby()
    {
        if (hostLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
            }
            catch (LobbyServiceException e) { Debug.LogError(e); }
        }
        SceneManager.LoadScene("MultiplayerScene");
    }


    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = 15f; 
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (hostLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer <= 0f)
            {
                lobbyUpdateTimer = 1.5f; 
                hostLobby = await LobbyService.Instance.GetLobbyAsync(hostLobby.Id);
                RefreshUI();
            }
        }
    }

    private void RefreshUI()
    {
        if (hostLobby == null) return;

        mapNameText.text = "Carte : " + hostLobby.Data["Map"].Value;
        maxPlayersText.text = "Places : " + hostLobby.Players.Count + " / " + hostLobby.MaxPlayers;

        string players = "Joueurs connectés :\n";
        foreach (var player in hostLobby.Players)
        {
            players += "- " + player.Id + "\n"; 
        }
        playerListText.text = players;
    }
}