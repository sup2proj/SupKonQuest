using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gère la création, la configuration et le maintien actif d'un salon (Lobby) multijoueur sur les serveurs d'Unity en tant qu'hôte.
/// </summary>
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

    /// <summary>
    /// Crée un nouveau salon public sur les serveurs d'Unity en utilisant le pseudo du joueur comme nom, et y associe les paramètres par défaut (Carte, etc.).
    /// </summary>
    private async System.Threading.Tasks.Task CreateLobby()
    {
        try
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Joueur Inconnu");
            string lobbyName = myName;
            
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


    /// <summary>
    /// Met à jour les données du salon sur le serveur pour changer la carte sélectionnée.
    /// </summary>
    /// <param name="newMap">Le nom de la nouvelle carte à appliquer.</param>
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

    /// <summary>
    /// Modifie le nombre maximum de joueurs autorisés à rejoindre ce salon et met à jour le serveur.
    /// </summary>
    /// <param name="newMax">La nouvelle limite de joueurs.</param>
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

    /// <summary>
    /// Supprime définitivement le salon des serveurs d'Unity et retourne au menu multijoueur.
    /// </summary>
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


    /// <summary>
    /// Envoie un signal (ping) au serveur Unity toutes les 15 secondes pour indiquer que l'hôte est toujours là, empêchant ainsi le serveur de fermer le salon pour inactivité.
    /// </summary>
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

    /// <summary>
    /// Interroge les serveurs d'Unity toutes les 1,5 secondes pour récupérer les dernières modifications du salon (ex: un nouveau joueur a rejoint ou quitté).
    /// </summary>
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

    /// <summary>
    /// Met à jour les textes de l'interface utilisateur avec les données actuelles du salon (carte, places disponibles, liste des identifiants des joueurs).
    /// </summary>
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