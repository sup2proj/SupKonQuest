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

    /// <summary>
    /// Indique si un joueur donné est géré par une IA (présence d'une instance).
    /// </summary>
    /// <param name="id">Identifiant du joueur à vérifier.</param>
    /// <returns>True si une IA existe pour ce joueur.</returns>
    public static bool IsAIPlayer(int id)
    {
        return instancesByPlayerId.ContainsKey(id);
    }

    /// <summary>
    /// Tente de récupérer l'instance d'IA associée à un joueur.
    /// </summary>
    /// <param name="id">Identifiant du joueur.</param>
    /// <param name="instance">Sortie contenant l'instance trouvée (ou null).</param>
    /// <returns>True si une instance a été trouvée.</returns>
    public static bool TryGetAIForPlayer(int id, out IAInstance instance)
    {
        return instancesByPlayerId.TryGetValue(id, out instance);
    }

    /// <summary>
    /// Vérifie si le joueur <paramref name="id"/> possède une IA avec la difficulté donnée.
    /// </summary>
    /// <param name="id">Identifiant du joueur.</param>
    /// <param name="difficulty">Difficulté à comparer.</param>
    /// <returns>True si l'IA du joueur a cette difficulté.</returns>
    public static bool IsDifficultyForPlayer(int id, int difficulty)
    {
        return instancesByPlayerId.TryGetValue(id, out IAInstance instance) && instance != null && instance.DifficultyIA == difficulty;
    }

    private float decisionTimer;
    private int playerId = -1;

    /// <summary>
    /// Configure cette instance d'IA avec un nouvel identifiant de joueur et une difficulté.
    /// </summary>
    /// <param name="newPlayerId">Nouvel identifiant du joueur (min 1).</param>
    /// <param name="newDifficulty">Nouvelle difficulté (clampée entre 1 et 2).</param>
    public void Configure(int newPlayerId, int newDifficulty)
    {
        configuredPlayerId = Mathf.Max(1, newPlayerId);
        difficultyIA = Mathf.Clamp(newDifficulty, 1, 2);
    }

    /// <summary>
    /// Initialisation au chargement : récupère les singletons et crée
    /// un composant de production si nécessaire.
    /// </summary>
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

    /// <summary>
    /// Démarrage de l'instance : enregistre l'IA pour le joueur, crée une session
    /// joueur et initialise la production si nécessaire.
    /// </summary>
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
                productionEasyNormal.Initialize(playerManager, structureManager, mapGenerator, playerId);
        }
    }

    /// <summary>
    /// Tick principal appelé chaque frame : maintient les références, met à jour
    /// la logique de production et gère le timer de décision de l'IA.
    /// </summary>
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
                productionEasyNormal.Initialize(playerManager, structureManager, mapGenerator, playerId);
                productionEasyNormal.SetMapReady(true);
                productionEasyNormal.Tick();
            }

            decisionTimer += Time.deltaTime;
            if (decisionTimer >= decisionInterval)
                decisionTimer = 0f;
        }
    }

    /// <summary>
    /// Nettoyage lors de la destruction : retire l'instance du dictionnaire global
    /// si elle correspond à cette IA.
    /// </summary>
    private void OnDestroy()
    {
        if (instancesByPlayerId.TryGetValue(playerId, out IAInstance current) && current == this)
        {
            instancesByPlayerId.Remove(playerId);
        }
    }
}