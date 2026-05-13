using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyRoomManager : MonoBehaviour
{
    public static bool IsHost = false;
    public static Lobby JoinedLobby = null;

    private Lobby currentLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    [Header("Interface (UI) Common")]
    public TextMeshProUGUI lobbyStatusText;
    public TextMeshProUGUI playerListText;
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI maxPlayersText;

    [Header("Interface (UI) Host only")]
    public GameObject hostControlsPanel;

    private string currentMap = "Europe";
    private int currentMaxPlayers = 4;

    async void Start()
    {
        if (IsHost)
        {
            hostControlsPanel.SetActive(true);
            lobbyStatusText.text = "Création du Lobby en cours...";
            await CreateLobby();
        }
        else
        {
            hostControlsPanel.SetActive(false);
            currentLobby = JoinedLobby;
            lobbyStatusText.text = "Connecté au salon";
            RefreshUI();
        }
    }

    void Update()
    {
        if (IsHost)
        {
            HandleLobbyHeartbeat();
        }
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

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, currentMaxPlayers, options);
            RefreshUI();
            lobbyStatusText.text = "Lobby Ouvert ! En attente de joueurs...";
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }


    public async void ChangeMap(string newMap)
    {
        if (currentLobby == null || !IsHost) return;

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
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public async void ChangeMaxPlayers(int newMax)
    {
        if (currentLobby == null || !IsHost) return;

        try
        {
            currentMaxPlayers = newMax;
            UpdateLobbyOptions options = new UpdateLobbyOptions { MaxPlayers = currentMaxPlayers };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }


    public async void LeaveLobby()
    {
        if (currentLobby != null)
        {
            try
            {
                if (IsHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (LobbyServiceException e) { Debug.LogError(e); }
        }
        SceneManager.LoadScene("MultiplayerScene");
    }


    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = 15f; 
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (currentLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer <= 0f)
            {
                lobbyUpdateTimer = 1.5f; 
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                RefreshUI();
            }
        }
    }

    private void RefreshUI()
    {
        if (currentLobby == null) return;

        mapNameText.text = "Carte : " + currentLobby.Data["Map"].Value;
        maxPlayersText.text = "Places : " + currentLobby.Players.Count + " / " + currentLobby.MaxPlayers;

        string players = "Joueurs connectés :\n";
        foreach (var player in currentLobby.Players)
        {
            players += "- " + player.Id + "\n"; 
        }
        playerListText.text = players;
    }
}