using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [SerializeField] private PlayerSession sessionPrefab;
    [SerializeField] private Transform sessionsRoot;

    private readonly Dictionary<int, PlayerSession> sessionsById = new Dictionary<int, PlayerSession>();

    [Header("Bootstrap (optionnel)")]
    [SerializeField, Min(0)] private int autoCreatePlayerCount = 2;
    [SerializeField, Min(0)] private int autoStartGold = 500;

    [Header("Active player (runtime)")]
    [SerializeField, Min(1)] private int activePlayerId = 1;
    public int ActivePlayerId => activePlayerId;
    
    private float timer = 0f;


    private void Awake()
    {
        Instance = this;

        for (int i = 1; i <= autoCreatePlayerCount; i++)
        {
            if (!sessionsById.ContainsKey(i))
                CreateSessionForPlayer(i, autoStartGold, startUnitCount: 0, startStructureCount: 0);
        }
    }

    private void Update()
    {
        int playerId = GetActivePlayerId();
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            timer = 0f;
            GetGoldForPlayer(playerId);
        }
    }

    public PlayerSession CreateSessionForPlayer(int id, int startGold = 500, int startUnitCount = 1, int startStructureCount = 1)
    {
        if (sessionsById.ContainsKey(id))
        {
            Debug.LogWarning($"Session deja existante pour le joueur {id}");
            return sessionsById[id];
        }

        PlayerSession newSession = Instantiate(sessionPrefab, sessionsRoot);
        newSession.name = $"PlayerSession_{id}";
        newSession.Init(id, startGold, startUnitCount, startStructureCount);

        sessionsById.Add(id, newSession);
        return newSession;
    }

    public void SetActivePlayer(int playerId)
    {
        if (!sessionsById.ContainsKey(playerId))
        {
            Debug.LogWarning($"[PlayerManager] SetActivePlayer: aucune session pour playerId={playerId} (création auto).", this);
            CreateSessionForPlayer(playerId, autoStartGold, startUnitCount: 0, startStructureCount: 0);
        }
        activePlayerId = playerId;
        var s = GetSession(activePlayerId);
    }

    public PlayerSession GetSession(int id)
    {
        sessionsById.TryGetValue(id, out PlayerSession session);
        return session;
    }

    public int GetActivePlayerId()
    {
        return activePlayerId;
    }

    public void GetGoldForPlayer(int playerId)
    {
        float goldAmount = 10;
        int structureAmount = 0;
        if (structureAmount > 0)
        {
            float multiplicator = structureAmount / 10f;
            goldAmount += structureAmount * multiplicator;
        }
        var pm = PlayerManager.Instance;
        var session = pm != null ? pm.GetSession(playerId) : null;
        if (session)
        {
            session.AddGold((int)goldAmount);
            if (StatisticsInterface.Instance != null)
            {
                StatisticsInterface.Instance.Refresh();
            }
        }
    }

}
