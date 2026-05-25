using UnityEngine;
using Unity.Services.Lobbies;

/// <summary>
/// Objet persistant (Singleton) chargé de maintenir le salon actif en envoyant des pings en arrière-plan, empêchant la fermeture de la partie par les serveurs lors des changements de scène.
/// </summary>
public class LobbyKeeper : MonoBehaviour
{
    public static LobbyKeeper Instance;
    
    private string currentLobbyId;
    private float heartbeatTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Enregistre l'identifiant du salon et démarre le compte à rebours pour l'envoi régulier des signaux de présence (heartbeat).
    /// </summary>
    /// <param name="lobbyId">L'identifiant unique du salon à maintenir ouvert.</param>
    public void StartKeepingLobbyAlive(string lobbyId)
    {
        currentLobbyId = lobbyId;
        heartbeatTimer = 15f; 
        Debug.Log("LobbyKeeper good");
    }

    /// <summary>
    /// Interrompt la boucle de maintien en vie, vide l'identifiant du salon et détruit cet objet de la scène.
    /// </summary>
    public void StopKeepingLobby()
    {
        currentLobbyId = null;
        Destroy(gameObject);
    }

    async void Update()
    {
        if (string.IsNullOrEmpty(currentLobbyId)) return;

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = 15f;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobbyId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning("LobbyKeeper (Avertissement) : " + e.Message);
            }
        }
    }
}