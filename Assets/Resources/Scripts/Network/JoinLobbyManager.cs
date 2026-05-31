using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère la recherche, l'affichage sous forme de liste et la connexion aux salons (lobbies) existants sur les serveurs d'Unity.
/// </summary>
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

    /// <summary>
    /// Lance une requête aux serveurs d'Unity pour récupérer jusqu'à 25 salons publics disposant d'au moins une place libre, puis déclenche la mise à jour de l'interface.
    /// </summary>
    public async void RefreshLobbyList()
    {
        if (statusText != null) statusText.SetDynamicTranslations("Searching for games...", "Recherche de parties...","Ricerca di partite...");
        
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

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

    /// <summary>
    /// Nettoie la liste actuelle à l'écran, puis instancie un nouveau bloc (prefab) pour chaque salon trouvé. Applique automatiquement la traduction du préfixe ("Salon de", "Lobby of") selon la langue du joueur.
    /// </summary>
    /// <param name="lobbies">La liste des salons renvoyée par la requête au serveur.</param>
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
            
            int currentLang = PlayerPrefs.GetInt("Language", 0);
            string prefix = "Lobby of "; // 0 = Anglais par défaut

            if (currentLang == 1) 
            {
                prefix = "Salon de ";    // 1 = Français
            }
            else if (currentLang == 2) 
            {
                prefix = "Lobby di ";    // 2 = Italien
            }

            texts[0].text = prefix + lobby.Name + " (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")";
            
            
            if (lobby.Data != null && lobby.Data.ContainsKey("Map"))
            {
                texts[1].text = "Carte : " + lobby.Data["Map"].Value;
            }

            Button joinBtn = item.GetComponentInChildren<Button>();
            joinBtn.onClick.AddListener(() => JoinLobby(lobby.Id));
        }
    }

    /// <summary>
    /// Tente de rejoindre un salon spécifique, prépare les données initiales du joueur (son pseudo et son statut "Non Prêt") et charge la scène de la salle d'attente en cas de succès.
    /// </summary>
    /// <param name="lobbyId">L'identifiant unique (ID) du salon que le joueur souhaite rejoindre.</param>
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

    /// <summary>
    /// Interrompt la recherche de salons et retourne à l'écran principal du multijoueur.
    /// </summary>
    public void BackToMenu()
    {
        SceneManager.LoadScene("MultiplayerScene");
    }
}