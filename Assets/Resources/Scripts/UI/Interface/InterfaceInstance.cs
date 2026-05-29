using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class InterfaceInstance : MonoBehaviour
{
    public static InterfaceInstance Instance { get; private set; }

    [Header("UI")]
    [SerializeField] public GameObject unitsQueue;
    [SerializeField] public GameObject unitsProtector;
    
    [Header("Queue item")]
    [SerializeField] private Vector3 queuedItemLocalScale = new Vector3(0.75f, 0.35f, 1f);
    [SerializeField] private Image[] queueSlots;

    [Header("Protector item")] 
    [SerializeField] private Image[] unitsProtectorSlots;
    
	[Header("Player Statistics")]
	[SerializeField] private StatisticsInterface statisticsInterface;
	[SerializeField] private GameObject tabMenuStatistics;
    [SerializeField] private GameObject exitMenu;
    
    [SerializeField] private bool isExitMenuActive = false;
    
    [Header("Buff (support/healer)")]
    [SerializeField] private Image[] buffSlots;
    
    [Header("Boat exit button")]
    [SerializeField] private Image boatExitButton;
    

    [Header("TEMPORAIRE JOUEUR LIST")] 
    [SerializeField] private List<Image> playersList;
    [SerializeField] private int activePlayerIndex = -1;

    [Header("Players (runtime)")]
    [SerializeField] private PlayerManager playerManager;

    [Header("PanelInMiddle")]
    [SerializeField] private PanelInMiddle panelInMiddle;

    public int ActivePlayerIndex => activePlayerIndex;
    public int ActivePlayerNumber => activePlayerIndex + 1;

    private struct ProtectorUiMapping
    {
        public int slotIndex;
        public int unitDataIndex;
        public int priceIndex;

        public ProtectorUiMapping(int slotIndex, int unitDataIndex, int priceIndex)
        {
            this.slotIndex = slotIndex;
            this.unitDataIndex = unitDataIndex;
            this.priceIndex = priceIndex;
        }
    }

    private static readonly Dictionary<int, UnitsType> ProtectorSlotToType = new Dictionary<int, UnitsType>
    {
        { 0, UnitsType.Infantry },
        { 1, UnitsType.Mortar },
        { 2, UnitsType.Heavy },
        { 3, UnitsType.Archer },
        { 4, UnitsType.AntiBlindage },
    };

    private static readonly Dictionary<UnitsType, ProtectorUiMapping> ProtectorUiByType = new Dictionary<UnitsType, ProtectorUiMapping>
    {
        { UnitsType.Infantry, new ProtectorUiMapping(0, 5, 4) },
        { UnitsType.Mortar, new ProtectorUiMapping(1, 6, 3) },
        { UnitsType.Heavy, new ProtectorUiMapping(2, 4, 2) },
        { UnitsType.Archer, new ProtectorUiMapping(3, 1, 1) },
        { UnitsType.AntiBlindage, new ProtectorUiMapping(4, 0, 0) },
    };

    private static readonly Dictionary<int, int> ProtectorSlotToIconButtonNumber = new Dictionary<int, int>
    {
        { 0, 5 },
        { 1, 6 },
        { 2, 4 },
        { 3, 2 },
        { 4, 1 },
    };

    private static readonly Dictionary<int, int> PlayerUiIndexToPlayerId = new Dictionary<int, int>
    {
        { 0, 1 },
        { 1, 2 },
        { 2, 3 },
    };

    [Header("Progression Bar")]
    [SerializeField] public ProgressBar progressBar;
    
    [Header("Runtime")]
    public static float currentProgression;
    
    private StructureInstance displayedStructure;
    private readonly Dictionary<int, List<Sprite>> queuedSpritesByStructure = new Dictionary<int, List<Sprite>>();
    private readonly Dictionary<int, BuildingCreationState> creationStateByStructure = new Dictionary<int, BuildingCreationState>();
    private int progressBarBoundStructureId = -1;
    private readonly Dictionary<int, Coroutine> buffSlotReappearCoroutines = new Dictionary<int, Coroutine>();

    private class BuildingCreationState
    {
        public readonly Queue<UnitCreationRequest> queue = new Queue<UnitCreationRequest>();
        public Coroutine coroutine;
        public bool hasCurrentRequest;
        public float currentCreationStartedAt;
        public float currentCreationDuration;
    }

    private struct UnitCreationRequest
    {
        public int unitIndex;
        public UnitsType type;
        public float x;
        public float z;
        public bool isPoweredUnit;
        public bool isProtector;
        public int buildingPlayerId;
        public int sourceStructureId;
    }
    
    /// <summary>
    /// Initialise l'instance singleton de l'InterfaceInstance.
    /// </summary>
    void Awake()
    {
        Instance = this;
    }

     /// <summary>
     /// Configuration initiale de l'interface : masque/initialise les éléments UI et attache les handlers.
     /// </summary>
     void Start()
     {
         if (unitsQueue != null && unitsProtector != null)
         {
             SetGameObjectActive(unitsQueue, false);
             SetGameObjectActive(unitsProtector, false);
         }
         
         if (unitsQueue == null && unitsProtector == null)
         {
             Debug.LogWarning("[InterfaceInstance] unitsQueue ou unitsprotector n'est pas assigné dans l'Inspector.");
             return;
         }

         ResolvePlayerManager();
         HideProgressBarVisual(resetProgress: false);
         hideBuffIcons();
         HideBoatExitIcons();
         WireBoatExitButtonClick();
         WireBuffSlotClicks();
         WirePlayersListClicks();
         WireProtectorSlotClicks();
         RefreshPlayerStatisticsUI();
         
         // Initialiser tabMenuStatistics masqué
         if (tabMenuStatistics != null)
             SetGameObjectActive(tabMenuStatistics, false);

        // Initialiser exitMenu masqué
        if (exitMenu != null)
            SetGameObjectActive(exitMenu, false);
     }

     /// <summary>
     /// Gestion des entrées utilisateur liées à l'interface (ex: touche TAB pour afficher les stats).
     /// </summary>
     void Update()
     {
         // Gestion de la touche TAB pour afficher/masquer les statistiques
        if (Input.GetKeyDown(KeyCode.Tab))
        {
             if (tabMenuStatistics != null)
                 SetGameObjectActive(tabMenuStatistics, true);
        }
         
        if (Input.GetKeyUp(KeyCode.Tab))
        {
             if (tabMenuStatistics != null)
                 SetGameObjectActive(tabMenuStatistics, false);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (exitMenu != null) {
                isExitMenuActive = !isExitMenuActive;
                SetGameObjectActive(exitMenu, isExitMenuActive);
            }
        }
     }

    public void CloseExitMenu()
    {
        isExitMenuActive = false;
        SetGameObjectActive(exitMenu, false);
    }

    /// <summary>
    /// Cache l'ensemble de l'interface HUD (structures, buffs, boutons, liste joueurs, statistiques).
    /// </summary>
    public void HideHud()
    {
        HideStructureInterface();
        hideBuffIcons();
        HideBoatExitIcons();
        HidePlayersList();
        HideStatisticsInterface();

        if (ActionInterface.Instance != null)
            ActionInterface.Instance.HideAllButtons();
    }

    /// <summary>
    /// Affiche le panneau de victoire pour le joueur gagnant si le panel central est prêt.
    /// </summary>
    public void ShowVictoryPanel(int winnerPlayerId)
    {
        if (!TryPreparePanelInMiddle())
            return;

        panelInMiddle.ShowVictory(winnerPlayerId);
    }

    /// <summary>
    /// Affiche le panneau de défaite pour le joueur défait si le panel central est prêt.
    /// </summary>
    public void ShowDefeatPanel(int defeatedPlayerId)
    {
        if (!TryPreparePanelInMiddle())
            return;

        panelInMiddle.ShowDefeat(defeatedPlayerId);
    }

    /// <summary>
    /// Masque le panneau central (victoire/défaite) s'il est résolu.
    /// </summary>
    public void HideVictoryPanel()
    {
        ResolvePanelInMiddle();

        if (panelInMiddle != null)
            panelInMiddle.Hide();
    }

    /// <summary>
    /// Prépare le panel central en cachant le HUD et en résolvant la référence au panel; retourne true si prêt.
    /// </summary>
    private bool TryPreparePanelInMiddle()
    {
        HideHud();
        ResolvePanelInMiddle();

        if (panelInMiddle == null || !panelInMiddle.HasAssignedPanelReferences)
        {
            Debug.LogWarning("[InterfaceInstance] panelInMiddle n'est pas assigne dans l'Inspector.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Rafraîchit l'interface des statistiques du joueur si elle est assignée.
    /// </summary>
    private void RefreshPlayerStatisticsUI()
    {
        if (statisticsInterface != null)
            statisticsInterface.Refresh();
    }

    /// <summary>
    /// Assure que la référence au PlayerManager est résolue en recherchant l'objet dans la scène si nécessaire.
    /// </summary>
    private PlayerManager ResolvePlayerManager()
    {
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        return playerManager;
    }

    /// <summary>
    /// Active ou désactive un GameObject si la référence n'est pas nulle.
    /// </summary>
    private void SetGameObjectActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    /// <summary>
    /// Active ou désactive l'objet d'une Image UI si la référence est valide.
    /// </summary>
    private void SetImageActive(Image image, bool active)
    {
        if (image != null)
            SetGameObjectActive(image.gameObject, active);
    }

    /// <summary>
    /// Active ou désactive un tableau d'Images UI en itérant sur chaque élément.
    /// </summary>
    private void SetImageArrayActive(Image[] images, bool active)
    {
        if (images == null)
            return;

        foreach (var image in images)
        {
            SetImageActive(image, active);
        }
    }

    /// <summary>
    /// Active ou désactive une liste d'Images UI en itérant sur chaque élément.
    /// </summary>
    private void SetImageListActive(List<Image> images, bool active)
    {
        if (images == null)
            return;

        foreach (var image in images)
        {
            SetImageActive(image, active);
        }
    }

    /// <summary>
    /// Associe une action UnityAction au clic d'une Image utilisée comme bouton.
    /// </summary>
    private void WireImageButton(Image image, UnityEngine.Events.UnityAction action)
    {
        if (image == null)
            return;

        var button = image.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

	//TEMPORAIRE ---------------------
    /// <summary>
    /// Attache les callbacks de clic aux éléments de la liste de joueurs temporaire.
    /// </summary>
    private void WirePlayersListClicks()
    {
        if (playersList == null || playersList.Count == 0)
            return;

        for (int i = 0; i < playersList.Count; i++)
        {
            var img = playersList[i];
            int capturedIndex = i;
            WireImageButton(img, () => SelectPlayerFromPlayersList(capturedIndex));
        }
    }

    /// <summary>
    /// Sélectionne un joueur à partir de l'index UI dans la liste et met à jour l'état du gestionnaire de joueurs.
    /// </summary>
    private void SelectPlayerFromPlayersList(int uiIndex)
    {
        int playerId;
        if (!PlayerUiIndexToPlayerId.TryGetValue(uiIndex, out playerId))
        {
            // Fallback générique: index 0 => player 1, index 1 => player 2, etc.
            playerId = uiIndex + 1;
        }

        activePlayerIndex = playerId - 1;

        ResolvePlayerManager();

        if (playerManager != null)
            playerManager.SetActivePlayer(playerId);

        if (Defeat.IsPlayerDefeated(playerId))
        {
            ShowDefeatPanel(playerId);
            return;
        }

        int gold = -1;
        if (playerManager != null)
        {
            var session = playerManager.GetSession(playerId);
            gold = session != null ? session.Gold : -1;
        }

        Debug.Log($"[InterfaceInstance] Switch joueur (UI): uiIndex={uiIndex} => playerId={playerId}, gold={gold}.", this);

        RefreshPlayerStatisticsUI();
    }

    /// <summary>
    /// Obtient l'ID du joueur actuellement sélectionné en priorisant le PlayerManager, sinon fallback.
    /// </summary>
    private int GetSelectedPlayerId()
    {
        ResolvePlayerManager();

        // Source de vérité: PlayerManager
        if (playerManager != null)
        {
            int id = playerManager.GetActivePlayerId();
            var session = playerManager.GetSession(id);
            int gold = session != null ? session.Gold : -1;
            Debug.Log($"[InterfaceInstance] Joueur actif (PlayerManager) utilisé: id={id}, gold={gold}.", this);
            return id;
        }

        // Fallback: si pas de PlayerManager, on peut retomber sur la structure.
        var selected = StructureInstance.CurrentlySelected;
        if (selected != null)
        {
            int id = selected.playerId + 1;
            return id;
        }

        Debug.Log("[InterfaceInstance] Aucun PlayerManager, fallback sur Player 1.", this);
        return 1;
    }
    /// <summary>
    /// Cache la liste UI des joueurs.
    /// </summary>
    private void HidePlayersList()
    {
        SetImageListActive(playersList, false);
    }

    /// <summary>
    /// Cache l'interface des statistiques si elle est assignée.
    /// </summary>
    private void HideStatisticsInterface()
    {
        if (statisticsInterface == null)
            return;

        SetGameObjectActive(statisticsInterface.gameObject, false);
    }

    /// <summary>
    /// Résout la référence au PanelInMiddle en cherchant dans la scène s'il n'est pas déjà assigné.
    /// </summary>
    private void ResolvePanelInMiddle()
    {
        if (panelInMiddle != null && panelInMiddle.HasAssignedPanelReferences)
            return;

        PanelInMiddle[] panels = FindObjectsByType<PanelInMiddle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null || !panels[i].HasAssignedPanelReferences)
                continue;

            panelInMiddle = panels[i];
            return;
        }

        if (panelInMiddle == null && panels.Length > 0)
            panelInMiddle = panels[0];
    }
}
