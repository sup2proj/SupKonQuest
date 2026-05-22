using System.Collections.Generic;
using UnityEngine;

public class IAInstance : MonoBehaviour
{
    private static readonly Dictionary<int, IAInstance> instancesByPlayerId = new Dictionary<int, IAInstance>();

    [Header("IA")] [SerializeField, Min(1f)]
    private float decisionInterval = 2f;

    [Header("Références")] [SerializeField]
    private PlayerManager playerManager;

    [SerializeField] private StructureManager structureManager;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private ProductionEasyNormal productionEasyNormal;
    [SerializeField] private int difficultyIA = 2;
    [SerializeField, Min(1)] private int configuredPlayerId = 2;

    // Propriété publique pour exposer la difficulté (lecture/écriture)
    public int DifficultyIA
    {
        get => difficultyIA;
        set => difficultyIA = value;
    }

    // Expose playerId so other systems can detect IA-owned structures/units
    public int PlayerId => playerId;

    public static bool IsAIPlayer(int id)
    {
        return instancesByPlayerId.ContainsKey(id);
    }

    public static bool TryGetAIForPlayer(int id, out IAInstance instance)
    {
        return instancesByPlayerId.TryGetValue(id, out instance);
    }

    public static bool IsDifficultyForPlayer(int id, int difficulty)
    {
        return instancesByPlayerId.TryGetValue(id, out IAInstance instance) && instance != null && instance.DifficultyIA == difficulty;
    }

    private float decisionTimer;
    private int playerId = -1;

    public void Configure(int newPlayerId, int newDifficulty)
    {
        configuredPlayerId = Mathf.Max(1, newPlayerId);
        difficultyIA = Mathf.Clamp(newDifficulty, 1, 2);
    }

    private void Awake()
    {
        if (playerManager == null)
            playerManager = PlayerManager.Instance;
        if (structureManager == null)
            structureManager = StructureManager.Instance;
        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance != null
                ? MapGenerator.Instance
                : FindFirstObjectByType<MapGenerator>();

        if (productionEasyNormal == null)
        {
            productionEasyNormal = GetComponent<ProductionEasyNormal>();
            if (productionEasyNormal == null)
            {
                productionEasyNormal = gameObject.AddComponent<ProductionEasyNormal>();
                Debug.Log("[AIEasy] ProductionEasyNormal créé automatiquement.");
            }
        }
    }

    private void Start()
    {
        playerId = Mathf.Max(1, configuredPlayerId);
        instancesByPlayerId[playerId] = this;

        if (playerManager != null)
        {
            playerManager.CreateSessionForPlayer(playerId, 500, startUnitCount: 0, startStructureCount: 1);
        }

        if (difficultyIA == 1 || difficultyIA == 2)
        {
            if (productionEasyNormal != null)
                productionEasyNormal.Initialize(playerManager, structureManager, mapGenerator, playerId, difficultyIA);
        }
    }

    private void Update()
    {
        if (playerManager == null)
            playerManager = PlayerManager.Instance;

        if (structureManager == null)
            structureManager = StructureManager.Instance;

        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance != null
                ? MapGenerator.Instance
                : FindFirstObjectByType<MapGenerator>();

        if (playerManager == null || structureManager == null)
            return;

        if (difficultyIA == 1 || difficultyIA == 2)
        {
            if (productionEasyNormal == null)
                productionEasyNormal = GetComponent<ProductionEasyNormal>();

            if (productionEasyNormal != null)
            {
                productionEasyNormal.Initialize(playerManager, structureManager, mapGenerator, playerId,
                    difficultyIA);
                productionEasyNormal.SetMapReady(true);
                productionEasyNormal.Tick();
            }

            decisionTimer += Time.deltaTime;
            if (decisionTimer >= decisionInterval)
                decisionTimer = 0f;
        }
    }

    private void OnDestroy()
    {
        if (instancesByPlayerId.TryGetValue(playerId, out IAInstance current) && current == this)
        {
            instancesByPlayerId.Remove(playerId);
        }
    }
}