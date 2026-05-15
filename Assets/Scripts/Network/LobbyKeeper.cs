using UnityEngine;
using Unity.Services.Lobbies;

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

    public void StartKeepingLobbyAlive(string lobbyId)
    {
        currentLobbyId = lobbyId;
        heartbeatTimer = 15f; 
        Debug.Log("LobbyKeeper good");
    }

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