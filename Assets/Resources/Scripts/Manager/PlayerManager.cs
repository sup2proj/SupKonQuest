using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private PlayerSession sessionPrefab;
    [SerializeField] private Transform sessionsRoot;

    private readonly Dictionary<int, PlayerSession> sessionsById = new Dictionary<int, PlayerSession>();

    [Header("Bootstrap (optionnel)")]
    [SerializeField, Min(0)] private int autoCreatePlayerCount = 2;
    [SerializeField, Min(0)] private int autoStartGold = 500;

    private void Awake()
    {
        // Auto-création simple pour éviter des sessions null en jeu.
        // Tu peux désactiver en mettant autoCreatePlayerCount = 0.
        for (int i = 1; i <= autoCreatePlayerCount; i++)
        {
            if (!sessionsById.ContainsKey(i))
                CreateSessionForPlayer(i, autoStartGold, startUnitCount: 0, startStructureCount: 0);
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

    public PlayerSession GetSession(int id)
    {
        sessionsById.TryGetValue(id, out PlayerSession session);
        return session;
    }
}
