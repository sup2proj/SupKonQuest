using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class LobbyRoomManager : MonoBehaviour
{
    public static bool IsHost = false;
    public static Lobby JoinedLobby = null;

    private Lobby currentLobby;
    private float lobbyUpdateTimer;

    [Header("Interface (UI) Common")]
    public LocalizedText lobbyStatusText;

    [Header("Players List (UI)")]
    public Transform playerListContainer;
    public GameObject playerListItemPrefab;

    public LocalizedText mapNameText;
    public LocalizedText maxPlayersText;
    public Button readyBtn;

    private bool isLocalPlayerReady = false;

    [Header("Interface (UI) Chat")]
    public UnityEngine.UI.ScrollRect chatScrollRect;
    public TextMeshProUGUI chatHistoryText;
    public TMP_InputField chatInputField;
    public Button sendChatBtn;

    [Header("Interface (UI) Host only")]
    public GameObject hostControlsPanel;
    public Button startGameBtn;

    private string currentMap = "Europe";
    private int currentMaxPlayers = 4;
    private Dictionary<string, string> lastProcessedMessages = new Dictionary<string, string>();

    async void Start()
    {
        if (IsHost)
        {
            hostControlsPanel.SetActive(true);
            if (lobbyStatusText != null) lobbyStatusText.SetDynamicTranslations("Creating Lobby...", "Création du Lobby en cours...");
                
            await CreateLobby();
        }
        else
        {
            hostControlsPanel.SetActive(false);
            currentLobby = JoinedLobby;
            if (lobbyStatusText != null) lobbyStatusText.SetDynamicTranslations("Connected to lobby", "Connecté au salon");
                
            RefreshUI();
        }
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(delegate 
            { 
                SendChatMessage(); 
                chatInputField.ActivateInputField(); 
            });
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
            
            // 0 = Anglais, 1 = Français
            int currentLang = PlayerPrefs.GetInt("Language", 0);
            string lobbyName = (currentLang == 0 ? "Lobby of " : "Salon de ") + myName;
            string welcomeMsg = (currentLang == 0 ? "Welcome to the lobby!\n" : "Bienvenue dans le salon !\n");
            
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
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, currentMap) },
                    { "ChatLog", new DataObject(DataObject.VisibilityOptions.Member, welcomeMsg) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, currentMaxPlayers, options);
            
            GameObject keeperObj = new GameObject("LobbyKeeper");
            LobbyKeeper keeper = keeperObj.AddComponent<LobbyKeeper>();
            keeper.StartKeepingLobbyAlive(currentLobby.Id);
            
            RefreshUI();
            
            if (lobbyStatusText != null) lobbyStatusText.SetDynamicTranslations("Lobby Open! Waiting for players...", "Lobby Ouvert ! En attente de joueurs...");
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

    public async void KickPlayer(string targetPlayerId)
    {
        if (!IsHost || currentLobby == null) return;

        try
        {
            Debug.Log("Éjection du joueur : " + targetPlayerId);
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, targetPlayerId);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public async void SendChatMessage()
    {
        if (currentLobby == null || string.IsNullOrWhiteSpace(chatInputField.text)) return;

        string myName = PlayerPrefs.GetString("PlayerName", "Joueur");
        string myMessage = chatInputField.text;
        chatInputField.text = ""; 

        try
        {
            string messageToPost = "<b>" + myName + " :</b> " + myMessage + "|" + System.DateTime.Now.Ticks.ToString();
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "LastMessage", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, messageToPost) }
                }
            };
            await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, options);
            chatInputField.ActivateInputField();
        }
        catch (LobbyServiceException e) { Debug.LogError("Erreur Chat : " + e.Message); }
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

            if (readyBtn != null)
            {
                LocalizedText btnText = readyBtn.GetComponentInChildren<LocalizedText>();
                if (btnText != null)
                {
                    if (isLocalPlayerReady)
                        btnText.SetDynamicTranslations("Cancel Ready", "Annuler Prêt");
                    else
                        btnText.SetDynamicTranslations("Ready", "Être Prêt");
                }
            }
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public async void StartNetworkGame()
    {
        if (!IsHost || currentLobby == null) return;

        Debug.Log("Création du serveur Relay en cours...");
        startGameBtn.interactable = false;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(currentMaxPlayers - 1);
            
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Serveur Relay créé ! Code : " + joinCode);

            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "True") },
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            
            // TODO: launch le reseau local et lance la scène
            Debug.Log("L'hôte est prêt à charger la GameScene");
        }
        catch (RelayServiceException e) { Debug.LogError("Erreur Relay : " + e.Message); }
        catch (LobbyServiceException e) { Debug.LogError("Erreur Lobby : " + e.Message); }
    }

    private async void CheckGameStartSignal()
    {
        if (currentLobby != null && currentLobby.Data != null)
        {
            if (currentLobby.Data.ContainsKey("GameStarted") && currentLobby.Data["GameStarted"].Value == "True")
            {
                lobbyUpdateTimer = 9999f; 

                string relayCode = currentLobby.Data["RelayCode"].Value;
                Debug.Log("j'ai j'ai ! Connexion au Relay avec code : " + relayCode);

                if (!IsHost)
                {
                    try
                    {
                        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
                        Debug.Log("Client connecté au Relay");
                        
                        // TODO: launch le reseau et lance la scène
                    }
                    catch (RelayServiceException e) { Debug.LogError("Erreur Relay Client : " + e.Message); }
                }
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
                
                try
                {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                    
                    bool amIStillInLobby = false;
                    string myId = AuthenticationService.Instance.PlayerId;
                    foreach (var p in currentLobby.Players)
                    {
                        if (p.Id == myId) amIStillInLobby = true;
                    }

                    if (!amIStillInLobby)
                    {
                        HandleDisconnection("Kicked by host.", "Vous avez été expulsé du salon par l'hôte.");
                        return; 
                    }

                    if (IsHost)
                    {
                        bool chatNeedsUpdate = false;
                        string currentChatLog = currentLobby.Data.ContainsKey("ChatLog") ? currentLobby.Data["ChatLog"].Value : "";

                        foreach (var player in currentLobby.Players)
                        {
                            if (player.Data != null && player.Data.ContainsKey("LastMessage"))
                            {
                                string rawMsg = player.Data["LastMessage"].Value;
                                
                                if (!lastProcessedMessages.ContainsKey(player.Id) || lastProcessedMessages[player.Id] != rawMsg)
                                {
                                    lastProcessedMessages[player.Id] = rawMsg;                                    
                                    string actualMessage = rawMsg.Substring(0, rawMsg.LastIndexOf('|'));
                                    currentChatLog += actualMessage + "\n";
                                    chatNeedsUpdate = true;
                                }
                            }
                        }
                        if (chatNeedsUpdate)
                        {
                            if (currentChatLog.Length > 800) currentChatLog = currentChatLog.Substring(currentChatLog.Length - 800);
                            UpdateLobbyOptions options = new UpdateLobbyOptions
                            {
                                Data = new Dictionary<string, DataObject>
                                {
                                    { "ChatLog", new DataObject(DataObject.VisibilityOptions.Member, currentChatLog) }
                                }
                            };
                            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
                        }
                    }
                    RefreshUI();
                    CheckGameStartSignal();
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogWarning("Impossible de rafraîchir le salon. Erreur : " + e.Reason);
                    HandleDisconnection("Connection lost.", "La connexion au salon a été perdue.");
                }
            }
        }
    }

    private void RefreshUI()
    {
        if (currentLobby == null) return;

        if (mapNameText != null)
            mapNameText.SetDynamicTranslations("Map: " + currentLobby.Data["Map"].Value, "Carte : " + currentLobby.Data["Map"].Value);
            
        if (maxPlayersText != null)
            maxPlayersText.SetDynamicTranslations("Slots: " + currentLobby.Players.Count + " / " + currentLobby.MaxPlayers, "Places : " + currentLobby.Players.Count + " / " + currentLobby.MaxPlayers);

        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        int readyCount = 0;
        string myId = AuthenticationService.Instance.PlayerId;

        foreach (var player in currentLobby.Players)
        {
            string playerName = player.Data != null && player.Data.ContainsKey("PlayerName") ? player.Data["PlayerName"].Value : player.Id;
            bool isReady = player.Data != null && player.Data.ContainsKey("IsReady") && player.Data["IsReady"].Value == "True";
            
            if (isReady) readyCount++;

            GameObject newPlayerItem = Instantiate(playerListItemPrefab, playerListContainer);
            PlayerListItem itemScript = newPlayerItem.GetComponent<PlayerListItem>();
            
            bool isMe = (player.Id == myId);
            itemScript.Setup(player.Id, playerName, isReady, IsHost, isMe);
        }

        if (IsHost && startGameBtn != null)
        {
            startGameBtn.interactable = (readyCount == currentLobby.Players.Count);
        }
        if (currentLobby.Data.ContainsKey("ChatLog"))
        {
            if (chatHistoryText.text != currentLobby.Data["ChatLog"].Value)
            {
                chatHistoryText.text = currentLobby.Data["ChatLog"].Value;
                Canvas.ForceUpdateCanvases();
                if (chatScrollRect != null)
                {
                    chatScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
    }

    private void HandleDisconnection(string reasonEN, string reasonFR)
    {
        Debug.LogWarning("Déconnexion forcée : " + reasonFR);
        
        currentLobby = null; 
        
        PlayerPrefs.SetString("DisconnectReasonEN", reasonEN);
        PlayerPrefs.SetString("DisconnectReasonFR", reasonFR);
        SceneManager.LoadScene("MultiplayerScene");
    }
}