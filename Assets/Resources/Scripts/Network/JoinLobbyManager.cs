using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class JoinLobbyManager : MonoBehaviour
{
    [Header("Interface (UI)")]
    public Transform lobbyListContainer;
    public GameObject lobbyItemPrefab;

    public LocalizedText statusText;

    void Start()
    {
        RefreshLobbyList();
    }

    public async void RefreshLobbyList()
    {
        if (statusText != null) statusText.SetDynamicTranslations("Searching for games...", "Recherche de parties...","Ricerca di partite...");
        
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25; // On veut maximum 25 résultats

            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (statusText != null) 
                statusText.SetDynamicTranslations(
                    response.Results.Count + " game(s) found", 
                    response.Results.Count + " partie(s) trouvée(s)",
                    response.Results.Count + " risultato(i) trovato(i)"
                );
            UpdateLobbyUI(response.Results);
        }
        catch (LobbyServiceException e)
        {
            if (statusText != null) statusText.SetDynamicTranslations("Search error.", "Erreur de recherche.","Errore di ricerca.");
            Debug.LogError(e);
        }
    }

    private void UpdateLobbyUI(List<Lobby> lobbies)
    {
        foreach (Transform child in lobbyListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbies)
        {
            GameObject item = Instantiate(lobbyItemPrefab, lobbyListContainer);
            
            TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            
            texts[0].text = lobby.Name + " (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")";
            
            if (lobby.Data != null && lobby.Data.ContainsKey("Map"))
            {
                texts[1].text = "Carte : " + lobby.Data["Map"].Value;
            }

            Button joinBtn = item.GetComponentInChildren<Button>();
            joinBtn.onClick.AddListener(() => JoinLobby(lobby.Id));
        }
    }

    public async void JoinLobby(string lobbyId)
    {
        if (statusText != null) statusText.SetDynamicTranslations("Connecting to lobby...", "Connexion au lobby...","Connessione alla lobby...");
        try
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Joueur Inconnu");

            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
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

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            Debug.Log("Lobby rejoint avec succès : " + joinedLobby.Name);
            
            LobbyRoomManager.IsHost = false;
            LobbyRoomManager.JoinedLobby = joinedLobby;
            SceneManager.LoadScene("LobbyRoomScene");
        }
        catch (LobbyServiceException e) { Debug.LogError(e); }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MultiplayerScene");
    }
}