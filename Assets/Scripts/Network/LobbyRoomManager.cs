using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyRoomManager : MonoBehaviour
{
    public static bool IsHost = false;
    public static Lobby JoinedLobby = null;

    private Lobby currentLobby;
    private float lobbyUpdateTimer;

    [Header("Interface (UI) Common")]
    public TextMeshProUGUI lobbyStatusText;
    public TextMeshProUGUI playerListText;
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI maxPlayersText;
    public Button readyBtn;

    private bool isLocalPlayerReady = false;

    [Header("Interface (UI) Host only")]
    public GameObject hostControlsPanel;
    public Button startGameBtn;

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
        HandleLobbyPolling();
    }


    private async System.Threading.Tasks.Task CreateLobby()
    {
        try
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Joueur Inconnu");
            string lobbyName = "Salon de " + myName;
            
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Unity.Services.Lobbies.Models.Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, myName) },
                        { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "False") }
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, currentMap) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, currentMaxPlayers, options);
            GameObject keeperObj = new GameObject("LobbyKeeper");
            LobbyKeeper keeper = keeperObj.AddComponent<LobbyKeeper>();
            keeper.StartKeepingLobbyAlive(currentLobby.Id);
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
        if (LobbyKeeper.Instance != null)
            {
                LobbyKeeper.Instance.StopKeepingLobby();
            }
        SceneManager.LoadScene("MultiplayerScene");
    }


    public async void ToggleReady()
    {
        if (currentLobby == null) return;

        isLocalPlayerReady = !isLocalPlayerReady;

        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isLocalPlayerReady.ToString()) }
                }
            };

            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, playerId, options);

            readyBtn.GetComponentInChildren<TextMeshProUGUI>().text = isLocalPlayerReady ? "Annuler Prêt" : "Être Prêt";
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public async void StartNetworkGame()
    {
        if (!IsHost || currentLobby == null) return;

        Debug.Log("Initialisation du réseau pour le lancement...");

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "True") }
                }
            };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    private void CheckGameStartSignal()
    {
        if (currentLobby != null && currentLobby.Data != null)
        {
            if (currentLobby.Data.ContainsKey("GameStarted") && currentLobby.Data["GameStarted"].Value == "True")
            {
                Debug.Log("lancement partie");
                
                // Scène de la partie
                // SceneManager.LoadScene("GameScene");
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
                CheckGameStartSignal();
            }
        }
    }

    private void RefreshUI()
    {
        if (currentLobby == null) return;

        mapNameText.text = "Carte : " + currentLobby.Data["Map"].Value;
        maxPlayersText.text = "Places : " + currentLobby.Players.Count + " / " + currentLobby.MaxPlayers;

        int readyCount = 0;
        string players = "Joueurs connectés :\n";
        
        foreach (var player in currentLobby.Players)
        {
            string readyStatus = "";
            
            string playerName = player.Id;
            
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                playerName = player.Data["PlayerName"].Value;
            }
            
            if (player.Data != null && player.Data.ContainsKey("IsReady") && player.Data["IsReady"].Value == "True")
            {
                readyStatus = " <color=#00FF00>[PRÊT]</color>"; 
                readyCount++;
            }
            
            players += "- " + playerName + readyStatus + "\n"; 
        }
        playerListText.text = players;

        if (IsHost && startGameBtn != null)
        {
            startGameBtn.interactable = readyCount == currentLobby.Players.Count;
        }
    }
}