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
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

/// <summary>
/// Gère la salle d'attente (Lobby Room) une fois qu'un joueur a créé ou rejoint une partie. Gère le chat, les paramètres de la partie, l'état "Prêt" des joueurs et la transition vers le jeu en réseau (Relay).
/// </summary>
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
            if (lobbyStatusText != null) lobbyStatusText.SetDynamicTranslations("Creating Lobby...", "Creation du Lobby en cours...","Creazione della lobby in corso...");
                
            await CreateLobby();
        }
        else
        {
            hostControlsPanel.SetActive(false);
            currentLobby = JoinedLobby;
            if (lobbyStatusText != null) lobbyStatusText.SetDynamicTranslations("Connected to lobby", "Connecte au salon","In collegamento con il salone");
                
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
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (chatInputField != null && !chatInputField.isFocused)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                chatInputField.Select();
                chatInputField.ActivateInputField();
            }
        }
    }

    /// <summary>
    /// Initialise un nouveau salon sur les serveurs d'Unity avec les données de départ (nom, carte, message de bienvenue) et assigne ce joueur comme hôte.
    /// </summary>
    private async System.Threading.Tasks.Task CreateLobby()
    {
        try
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Joueur Inconnu");
            
            int currentLang = PlayerPrefs.GetInt("Language", 0);
            
            string lobbyName = myName;
            string welcomeMsg = "";

            if (currentLang == 1) // Français
            {
                welcomeMsg = "Bienvenue dans le salon !\n";
            }
            else if (currentLang == 2) // Italiano
            {
                welcomeMsg = "Benvenuti in salotto !\n";
            }
            else // Anglais (si currentLang == 0 ou autre)
            {
                welcomeMsg = "Welcome to the lobby !\n";
            }
            
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
            
            if (lobbyStatusText != null) lobbyStatusText.SetDynamicTranslations("Lobby Open ! Waiting for players...", "Lobby Ouvert ! En attente de joueurs...","Lobby aperta ! In attesa dei giocatori...");
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    /// <summary>
    /// Permet à l'hôte de modifier la carte de la partie et met à jour cette information sur le serveur pour tous les joueurs.
    /// </summary>
    /// <param name="newMap">Le nom de la nouvelle carte sélectionnée.</param>
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
    
    /// <summary>
    /// Augmente la limite maximum de joueurs dans le salon (jusqu'à un maximum de 8). Action réservée à l'hôte.
    /// </summary>
    public async void UpMaxPlayers()
    {
        if (currentLobby == null || !IsHost) return;
        if (currentMaxPlayers >= 8) return; 
        try
        {
            currentMaxPlayers++; 
            UpdateLobbyOptions options = new UpdateLobbyOptions { MaxPlayers = currentMaxPlayers };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }
    
    /// <summary>
    /// Diminue la limite maximum de joueurs dans le salon (jusqu'à un minimum de 2). Action réservée à l'hôte.
    /// </summary>
    public async void DownMaxPlayers()
    {
        if (currentLobby == null || !IsHost) return;
        if (currentMaxPlayers <= 2) return; 
        try
        {
            currentMaxPlayers--; 
            UpdateLobbyOptions options = new UpdateLobbyOptions { MaxPlayers = currentMaxPlayers };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    /// <summary>
    /// Permet à l'hôte d'expulser un joueur spécifique du salon.
    /// </summary>
    /// <param name="targetPlayerId">L'identifiant (ID) du joueur à expulser.</param>
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

    /// <summary>
    /// Envoie un message dans le chat en mettant à jour les données du joueur avec le contenu du message et un horodatage (pour forcer la détection de la mise à jour par les autres joueurs).
    /// </summary>
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

    /// <summary>
    /// Quitte la salle d'attente. Si le joueur est l'hôte, le salon entier est détruit. Si c'est un client, il est simplement retiré de la liste.
    /// </summary>
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

    /// <summary>
    /// Alterne l'état du joueur local entre "Prêt" et "Non Prêt", et met à jour son statut sur le serveur pour débloquer le bouton de lancement de l'hôte.
    /// </summary>
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
                        btnText.SetDynamicTranslations("Cancel Ready", "Annuler Pret","Annulla Pronto");
                    else
                        btnText.SetDynamicTranslations("Ready", "Pret","Pronto");
                }
            }
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    /// <summary>
    /// Action réservée à l'hôte. Crée un serveur Relay pour héberger la vraie partie en réseau, génère un code de connexion et l'inscrit dans les données du salon pour y inviter automatiquement les autres joueurs.
    /// </summary>
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

            // Graine aléatoire pour la map
            int randomSeed = UnityEngine.Random.Range(10000, 99999);

            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "True") },
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
                    { "MapSeed", new DataObject(DataObject.VisibilityOptions.Member, randomSeed.ToString()) } // On sauvegarde la graine
                }
            };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            
            RelayServerData relayServerData = new RelayServerData(allocation, "udp");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            
            // L'hôte récupère le nom de la carte et transmet la graine générée
            string mapFolder = currentLobby.Data.ContainsKey("Map") ? currentLobby.Data["Map"].Value : "TEST";
            AutoLauncher.Request(mapFolder, 0, 2, randomSeed);           
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        catch (RelayServiceException e) { Debug.LogError("Erreur Relay : " + e.Message); }
        catch (LobbyServiceException e) { Debug.LogError("Erreur Lobby : " + e.Message); }
    }

    /// <summary>
    /// Vérifie si l'hôte a lancé la partie et publié le code Relay. Si c'est le cas, connecte automatiquement le client au serveur Relay.
    /// </summary>
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
                    
                        RelayServerData relayServerData = new RelayServerData(joinAllocation, "udp");
                        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                    
                        // Le Client lit la carte et la graine depuis les données du serveur
                        string mapFolder = currentLobby.Data.ContainsKey("Map") ? currentLobby.Data["Map"].Value : "TEST";
                        int mapSeed = currentLobby.Data.ContainsKey("MapSeed") ? int.Parse(currentLobby.Data["MapSeed"].Value) : -1;
                        AutoLauncher.Request(mapFolder, 0, 2, mapSeed);
                        NetworkManager.Singleton.StartClient();
                    }
                    catch (RelayServiceException e) { Debug.LogError("Erreur Relay Client : " + e.Message); }
                }
            }
        }
    }


    /// <summary>
    /// Interroge le serveur régulièrement pour mettre à jour la liste des joueurs, synchroniser l'historique du chat, et vérifier si le joueur a été expulsé ou si la partie a commencé.
    /// </summary>
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
                        HandleDisconnection("Kicked by host.", "Vous avez ete expulse du salon par l'hôte.","Sei stato espulso dalla chat dall'amministratore.");
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
                    HandleDisconnection("Connection lost.", "La connexion au salon a été perdue.","La connessione con la sala è stata interrotta.");
                }
            }
        }
    }

    /// <summary>
    /// Met à jour toute l'interface visuelle (liste des joueurs, état du bouton "Lancer", chat, carte) en fonction des dernières données récupérées du serveur.
    /// </summary>
    private void RefreshUI()
    {
        if (currentLobby == null) return;

        if (mapNameText != null)
            mapNameText.SetDynamicTranslations("Map: " + currentLobby.Data["Map"].Value, "Carte : " + currentLobby.Data["Map"].Value, "Mappa : "+currentLobby.Data["Map"].Value);
            
        if (maxPlayersText != null)
            maxPlayersText.SetDynamicTranslations("Slots: " + currentLobby.Players.Count + " / " + currentLobby.MaxPlayers, "Places : " + currentLobby.Players.Count + " / " + currentLobby.MaxPlayers,"Posti : "+currentLobby.Players.Count + " / " + currentLobby.MaxPlayers);

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

    /// <summary>
    /// Gère le retour forcé au menu multijoueur (en cas d'expulsion ou de perte de connexion) et sauvegarde la raison exacte pour l'afficher proprement au joueur à son retour au menu.
    /// </summary>
    /// <param name="reasonEN">Raison de la déconnexion en anglais.</param>
    /// <param name="reasonFR">Raison de la déconnexion en français.</param>
    /// <param name="reasonIT">Raison de la déconnexion en italien.</param>
    private void HandleDisconnection(string reasonEN, string reasonFR, string reasonIT)
    {
        Debug.LogWarning("Déconnexion forcée : " + reasonFR);
        
        currentLobby = null; 
        
        PlayerPrefs.SetString("DisconnectReasonEN", reasonEN);
        PlayerPrefs.SetString("DisconnectReasonFR", reasonFR);
        PlayerPrefs.SetString("DisconnectReasonIT", reasonIT);
        SceneManager.LoadScene("MultiplayerScene");
    }
}