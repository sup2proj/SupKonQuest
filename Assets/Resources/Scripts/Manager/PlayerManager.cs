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
    public int ActivePlayerId => activePlayerId;
    
    private float timer = 0f;


    private void Awake()
    {
        Instance = this;

        for (int i = 1; i <= autoCreatePlayerCount; i++)
        {
            if (!sessionsById.ContainsKey(i))
                CreateSessionForPlayer(i, autoStartGold, startUnitCount: 0, startStructureCount: 2);
        }
        //TEMPORAIRRRREEEE
        if (!sessionsById.ContainsKey(1))
            CreateSessionForPlayer(1, autoStartGold, startUnitCount: 0, startStructureCount: 2);

        SetActivePlayer(1);

        // Instancier l'IA via reflection pour éviter une dépendance forte au type AIEasy
        Type aiType = FindTypeInAssemblies("IAInstance");
        if (aiType != null)
        {
            // Vérifier s'il existe déjà une instance
            var existing = FindFirstObjectByType(aiType, FindObjectsInactive.Include);
            if (existing == null)
            {
                GameObject aiGO = new GameObject("IAInstance_Player2");
                var comp = aiGO.AddComponent(aiType);
                Debug.Log("[PlayerManager] IAInstance créée automatiquement pour le joueur IA (difficulty=2).");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerManager] Type 'IAInstance' introuvable dans les assemblies — l'IA ne sera pas instanciée automatiquement.");
        }
    }

    private void Start()
    {
        SetActivePlayer(1);
    }
        //TEMPORAIRRRREEEE

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

    public PlayerSession CreateSessionForPlayer(int id, int startGold = 500, int startUnitCount = 1, int startStructureCount = 2)
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
            CreateSessionForPlayer(playerId, autoStartGold, startUnitCount: 0, startStructureCount: 2);
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
        int structureAmount = GetSession(playerId).StructureCount;
        if (structureAmount > 0)
        {
            float multiplicator = (structureAmount / 10f) + 1;
            goldAmount *= multiplicator;
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

    // Recherche par nom de type dans tous les assemblies chargés
    private Type FindTypeInAssemblies(string typeName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            try
            {
                var t = asm.GetType(typeName);
                if (t != null)
                    return t;

                // essayer de retrouver par nom simple
                foreach (var tp in asm.GetTypes())
                {
                    if (tp.Name == typeName)
                        return tp;
                }
            }
            catch
            {
                // ignorer les assemblys qui posent problème
            }
        }
        return null;
    }

    // Fournit une couleur de sélection/identification pour un playerId
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
            default: return Color.gray;
        }
    }

}
