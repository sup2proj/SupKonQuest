using UnityEngine;
using System.Collections.Generic;
using System;

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
    private float timer = 0f;


    /// <summary>
    /// Initialise les sessions de joueurs et configure l'IA automatiquement si nécessaire.
    /// </summary>
    private void Awake()
    {
        Instance = this;

        for (int i = 1; i <= autoCreatePlayerCount; i++)
        {
            if (!sessionsById.ContainsKey(i))
                CreateSessionForPlayer(i, autoStartGold, startUnitCount: 0, startStructureCount: 1);
        }
        // Create default sessions based on autoCreatePlayerCount and set active player to 1.
        SetActivePlayer(1);
    }

    /// <summary>
    /// Accorde périodiquement de l'or au joueur actif tant qu'il n'est pas vaincu.
    /// </summary>
    private void Update()
    {
        int playerId = GetActivePlayerId();
        if (Defeat.IsPlayerDefeated(playerId))
            return;

        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            timer = 0f;
            GetGoldForPlayer(playerId);
        }
    }

    /// <summary>
    /// Crée une session pour un joueur si elle n'existe pas déjà.
    /// </summary>
    public PlayerSession CreateSessionForPlayer(int id, int startGold = 500, int startUnitCount = 1, int startStructureCount = 1)
    {
        if (sessionsById.ContainsKey(id))
        {
            return sessionsById[id];
        }

        PlayerSession newSession = Instantiate(sessionPrefab, sessionsRoot);
        newSession.name = $"PlayerSession_{id}";
        newSession.Init(id, startGold, startUnitCount, startStructureCount);

        sessionsById.Add(id, newSession);
        return newSession;
    }

    /// <summary>
    /// Définit l'identifiant du joueur actuellement actif.
    /// </summary>
    public void SetActivePlayer(int playerId)
    {
        if (!sessionsById.ContainsKey(playerId))
        {
            CreateSessionForPlayer(playerId, autoStartGold, startUnitCount: 0, startStructureCount: 0);
        }
        activePlayerId = playerId;
        var s = GetSession(activePlayerId);
    }

    /// <summary>
    /// Récupère la session associée à un identifiant de joueur.
    /// </summary>
    public PlayerSession GetSession(int id)
    {
        sessionsById.TryGetValue(id, out PlayerSession session);
        return session;
    }

    /// <summary>
    /// Retourne l'identifiant du joueur actif.
    /// </summary>
    public int GetActivePlayerId()
    {
        return activePlayerId;
    }

    /// <summary>
    /// Calcule et ajoute le gain d'or périodique à un joueur.
    /// </summary>
    public void GetGoldForPlayer(int playerId)
    {
        if (Defeat.IsPlayerDefeated(playerId))
            return;

        float goldAmount = 5;
        PlayerSession session = GetSession(playerId);
        if (session == null)
            return;

        int structureAmount = session.StructureCount;
        if (structureAmount > 0)
        {
            float goldMultiplicatorBonus = (structureAmount / 10f) + 2;
            goldAmount *= goldMultiplicatorBonus;
        }
        var pm = PlayerManager.Instance;
        session = pm != null ? pm.GetSession(playerId) : null;
        if (session)
        {
            session.AddGold((int)goldAmount);
            if (StatisticsInterface.Instance != null)
            {
                StatisticsInterface.Instance.Refresh();
            }
        }
    }

    /// <summary>
    /// Fournit une couleur associée à un identifiant de joueur.
    /// </summary>
    public static Color GetPlayerColor(int id)
    {
        switch (id)
        {
            case 1: return Color.white;
            case 2: return Color.red;
            case 3: return Color.blue;
            case 4: return Color.green;
            case 5: return Color.yellow;
            case 6: return Color.cyan;
            case 7: return new Color(1f, 0.5f, 0f, 1f); // orange
            case 8: return new Color(0.6f, 0f, 1f, 1f); // violet
            default: return Color.hotPink;
        }
    }

}
