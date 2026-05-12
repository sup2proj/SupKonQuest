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

    public TextMeshProUGUI statusText;

    void Start()
    {
        RefreshLobbyList();
    }

    public async void RefreshLobbyList()
    {
        statusText.text = "Recherche de parties...";
        
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25; // On veut maximum 25 résultats

            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            statusText.text = response.Results.Count + " partie(s) trouvée(s)";
            UpdateLobbyUI(response.Results);
        }
        catch (LobbyServiceException e)
        {
            statusText.text = "Erreur de recherche.";
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
        statusText.text = "Connexion au lobby...";
        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log("Lobby rejoint avec succès : " + joinedLobby.Name);
            
            // Plus tard, on chargera la scène de la salle d'attente ici !
            // SceneManager.LoadScene("LobbyRoomScene"); 
            statusText.text = "Succès ! Vous êtes dans le lobby.";
        }
        catch (LobbyServiceException e)
        {
            statusText.text = "Impossible de rejoindre (partie pleine ou fermée).";
            Debug.LogError(e);
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MultiplayerScene");
    }
}