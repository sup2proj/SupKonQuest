using System.Collections.Generic;
using UnityEngine;

public class IAInstance : MonoBehaviour
{
    [Header("IA")] [SerializeField, Min(1f)]
    private float decisionInterval = 2f;

    [Header("Références")] [SerializeField]
    private PlayerManager playerManager;

    [SerializeField] private StructureManager structureManager;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private ProductionEasyNormal productionEasyNormal;
    [SerializeField] private int difficultyIA = 2;

    // Propriété publique pour exposer la difficulté (lecture/écriture)
    public int DifficultyIA
    {
        get => difficultyIA;
        set => difficultyIA = value;
    }

    private float decisionTimer;
    private int playerId = -1;

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
        if (playerManager != null)
        {
            // L'IA est toujours le joueur 2, le joueur humain est le joueur 1
            playerId = 1;
            playerManager.CreateSessionForPlayer(playerId, 500, startUnitCount: 0, startStructureCount: 2);
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
}