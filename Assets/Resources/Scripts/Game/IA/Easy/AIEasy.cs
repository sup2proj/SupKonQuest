using UnityEngine;
using System.Collections.Generic;

public class AIEasy : MonoBehaviour
{
    [Header("IA Ultra Facile")]
    [SerializeField, Min(1f)] private float decisionInterval = 2f;

    [Header("Références")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private StructureManager structureManager;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private EasyProduction easyProduction;

    private float decisionTimer;
    private int playerId = -1;

    /// <summary>
    /// Initialisation des références singletons et création automatique du
    /// composant <see cref="EasyProduction"/> si nécessaire.
    /// </summary>
    private void Awake()
    {
        if (playerManager == null)
            playerManager = PlayerManager.Instance;
        if (structureManager == null)
            structureManager = StructureManager.Instance;
        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();

        // Créer automatiquement EasyProduction s'il n'existe pas
        if (easyProduction == null)
        {
            easyProduction = GetComponent<EasyProduction>();
            if (easyProduction == null)
            {
                easyProduction = gameObject.AddComponent<EasyProduction>();
                Debug.Log("[AIEasy] EasyProduction créé automatiquement.");
            }
        }
    }

    /// <summary>
    /// Démarrage : récupère l'identifiant du joueur actif et initialise la
    /// production de l'IA si le composant existe.
    /// </summary>
    private void Start()
    {
        if (playerManager != null)
            playerId = playerManager.GetActivePlayerId();

        if (easyProduction != null)
            easyProduction.Initialize(playerManager, structureManager, mapGenerator, playerId);
    }

    /// <summary>
    /// Tick principal : maintient les références, initialise la production
    /// et exécute la logique de production périodique de l'IA ultra-facile.
    /// </summary>
    private void Update()
    {
        if (playerManager == null)
            playerManager = PlayerManager.Instance;

        if (structureManager == null)
            structureManager = StructureManager.Instance;

        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();

        if (playerManager == null || structureManager == null)
            return;

        if (playerId < 0)
            playerId = playerManager.GetActivePlayerId();

        if (easyProduction == null)
            easyProduction = GetComponent<EasyProduction>();

        if (easyProduction != null)
        {
            easyProduction.Initialize(playerManager, structureManager, mapGenerator, playerId);
            easyProduction.SetMapReady(true);
            easyProduction.Tick();
        }

        decisionTimer += Time.deltaTime;
        if (decisionTimer >= decisionInterval)
            decisionTimer = 0f;
    }
}
