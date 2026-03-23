using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class InterfaceInstance : MonoBehaviour
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
    
    [Header("TEMPORAIRE JOUEUR LIST")] 
    [SerializeField] private List<Image> playersList;
    [SerializeField] private int activePlayerIndex = -1;

    [Header("Players (runtime)")]
    [SerializeField] private PlayerManager playerManager;

    public int ActivePlayerIndex => activePlayerIndex;
    public int ActivePlayerNumber => activePlayerIndex + 1;

    private static readonly Dictionary<int, UnitsType> ProtectorSlotToType = new Dictionary<int, UnitsType>
    {
        { 0, UnitsType.Infantry },
        { 1, UnitsType.Mortar },
        { 2, UnitsType.Heavy },
        { 3, UnitsType.Archer },
        { 4, UnitsType.AntiBlindage },
    };

    // Mapping des boutons UI -> playerId. Par défaut, index i => player i+1.
    // Tu peux le surcharger ici si tes boutons ne sont pas dans l'ordre.
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
    
    private int slotAvailabel = -1;
    
    private bool isSpawning = false;

    private struct UnitCreationRequest
    {
        public int unitIndex;
        public UnitsType type;
        public float x;
        public float z;
        public bool isPoweredUnit;
        public int playerId;
        public int paidCost;
    }
    
    private readonly Queue<UnitCreationRequest> creationQueue = new Queue<UnitCreationRequest>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (unitsQueue != null && unitsProtector != null)
        {
            unitsQueue.SetActive(false);
            unitsProtector.SetActive(false);
        }
        
        if (unitsQueue == null && unitsProtector == null)
        {
            Debug.LogWarning("[InterfaceInstance] unitsQueue ou unitsprotector n'est pas assigné dans l'Inspector.");
            return;
        }

        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        WirePlayersListClicks();
        WireProtectorSlotClicks();
    }

    void Update()
    {

    }

    public void showInterfaceForStructure()
    {
        unitsQueue.SetActive(true);
        unitsProtector.SetActive(true);
        hideUnitsProtectorSlots();
        // ClearFirstQueueSlot();
        
        RefreshQueueSlotsVisibility();
    }

    public void HideStructureInterface()
    {
        unitsQueue.SetActive(false);
        unitsProtector.SetActive(false);
        SetAllQueueSlotsActive(false);
    }

    public void addUnitToQueue(GameObject clickedUnit)
    {
        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (queueSlots[i] != null && queueSlots[i].sprite == null)
            {
                FillSlotImage(i, clickedUnit);
                slotAvailabel = i + 1;
                RefreshQueueSlotsVisibility();
                return;
            }
        }
    }

    public void FillSlotImage(int slotIndex, GameObject clickedUnit)
    {
        if (queueSlots == null || slotIndex < 0 || slotIndex >= queueSlots.Length)
            return;

        if (clickedUnit == null)
            return;

        var target = queueSlots[slotIndex];
        if (target == null)
            return;

        var source = clickedUnit.GetComponentInChildren<Image>(true);
        if (source == null)
            return;

        target.sprite = source.sprite;
    }
    
    public bool InitUnitsCreation(int unitIndex, UnitsType type, float x, float z, bool isPoweredUnit)
    {
        int playerId = GetSelectedPlayerId();
        var actionInterface = ActionInterface.Instance;
        UnitData unitData = actionInterface.unitDatas[unitIndex];
        float multiplier = isPoweredUnit ? 1.20f : 1f;
        int cost = Mathf.RoundToInt(unitData.price * multiplier);
        cost = Mathf.Max(0, cost);

        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();
        PlayerSession session = playerManager.GetSession(playerId);
        if (!session.SpendGold(cost))
        {
            Debug.Log($"[InterfaceInstance] Pas assez d'or pour demander la création: joueur={playerId}, gold={session.Gold}, coût={cost}.", this);
            return false;
        }

        Debug.Log($"[InterfaceInstance] Création demandée et payée: joueur={playerId}, coût={cost}, goldRestant={session.Gold}, type={type}, powered={isPoweredUnit}.", this);
        creationQueue.Enqueue(new UnitCreationRequest
        {
            unitIndex = unitIndex,
            type = type,
            x = x,
            z = z,
            isPoweredUnit = isPoweredUnit,
            playerId = playerId,
            paidCost = cost,
        });

        if (!isSpawning)
            StartCoroutine(ProcessCreationQueue());

        return true;
    }

	//TEMPORAIRE ---------------------
    private void WirePlayersListClicks()
    {
        if (playersList == null || playersList.Count == 0)
            return;

        for (int i = 0; i < playersList.Count; i++)
        {
            var img = playersList[i];
            if (img == null) continue;

            var btn = img.GetComponent<Button>();
            int capturedIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectPlayerFromPlayersList(capturedIndex));
        }
    }

    private void SelectPlayerFromPlayersList(int uiIndex)
    {
        int playerId;
        if (!PlayerUiIndexToPlayerId.TryGetValue(uiIndex, out playerId))
        {
            // Fallback générique: index 0 => player 1, index 1 => player 2, etc.
            playerId = uiIndex + 1;
        }

        activePlayerIndex = playerId - 1;

        int gold = -1;
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        if (playerManager != null)
        {
            var session = playerManager.GetSession(playerId);
            gold = session != null ? session.Gold : -1;
        }

        Debug.Log($"[InterfaceInstance] Switch joueur: uiIndex={uiIndex} => playerId={playerId}, gold={gold}.", this);
    }

    private int GetSelectedPlayerId()
    {
        // IMPORTANT: pour simuler 'je suis ce joueur', on prend le joueur actif UI.
        // (Sinon la structure sélectionnée forcerait l'id et tu ne pourrais pas agir "en tant que" un autre joueur.)
        if (activePlayerIndex >= 0)
        {
            int id = activePlayerIndex + 1;
            int gold = -1;
            if (playerManager != null)
            {
                var session = playerManager.GetSession(id);
                gold = session != null ? session.Gold : -1;
            }
            Debug.Log($"[InterfaceInstance] Joueur actif UI utilisé: id={id}, gold={gold}.", this);
            return id;
        }

        // Fallback: si aucun joueur n'a été sélectionné, on peut retomber sur la structure.
        var selected = StructureInstance.CurrentlySelected;
        if (selected != null)
        {
            int id = ((int)selected.player) + 1;
            return id;
        }

        Debug.Log("[InterfaceInstance] Aucun joueur sélectionné, fallback sur Player 1.", this);
        return 1;
    }
    // TEMPORAIRE -----------------------------------------------------------------------------

    private IEnumerator ProcessCreationQueue()
    {
        isSpawning = true;

        while (creationQueue.Count > 0)
        {
            UnitCreationRequest req = creationQueue.Dequeue();

            var actionInterface = ActionInterface.Instance;
            float creationTime = actionInterface.unitDatas[req.unitIndex].creationTime;

            if (progressBar != null)
            {
                progressBar.StartCreation(creationTime);
                progressBar.transform.localPosition = (1.1f * Vector3.up);
            }

            float elapsed = 0f;
            while (elapsed < creationTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (progressBar != null)
                progressBar.StopCreation(resetToZero: true);
            ShiftQueueLeft();
        }

        isSpawning = false;
    }

    private void ClearFirstQueueSlot()
    {
        if (queueSlots == null || queueSlots.Length == 0)
            return;

        if (queueSlots[0] != null)
            queueSlots[0].sprite = null;

        RefreshQueueSlotsVisibility();
    }

    private void ShiftQueueLeft()
    {
        if (queueSlots == null || queueSlots.Length == 0)
            return;

        for (int i = 0; i < queueSlots.Length - 1; i++)
        {
            if (queueSlots[i] == null) continue;
            var nextSprite = queueSlots[i + 1] != null ? queueSlots[i + 1].sprite : null;
            queueSlots[i].sprite = nextSprite;
        }

        if (queueSlots[^1] != null)
            queueSlots[^1].sprite = null;

        if (slotAvailabel > 0)
            slotAvailabel--;
        if (slotAvailabel < -1)
            slotAvailabel = -1;

        RefreshQueueSlotsVisibility();
    }

    private void SetAllQueueSlotsActive(bool active)
    {
        if (queueSlots == null) return;

        foreach (var img in queueSlots)
        {
            if (img == null) continue;
            img.gameObject.SetActive(active);
        }
    }

    private void RefreshQueueSlotsVisibility()
    {
        if (queueSlots == null) return;

        foreach (var img in queueSlots)
        {
            if (img == null) continue;
            bool hasSprite = img.sprite != null;
            img.gameObject.SetActive(hasSprite);
        }
    }

    private void hideUnitsProtectorSlots()
    {
        foreach (var img in unitsProtectorSlots)
        {
            if (img == null) continue;
            img.gameObject.SetActive(false);
        }
    }

    private void WireProtectorSlotClicks()
    {
        for (int i = 0; i < unitsProtectorSlots.Length; i++)
        {
            var img = unitsProtectorSlots[i];
            if (img == null) continue;

            var btn = img.GetComponent<Button>();
            int capturedIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SpawnStructProtectorOnInterface(capturedIndex));
        }
    }

    private void SpawnStructProtectorOnInterface(int slotIndex)
    {
        ProtectorSlotToType.TryGetValue(slotIndex, out var type);
        var selected = StructureInstance.CurrentlySelected;
        Vector3 pos = selected.StructurePosition;
        int playerId = GetSelectedPlayerId();
        StructureManager.Instance.SpawnUnitByTypeAtPosition(playerId, type, pos.x + 1f, pos.z + 1f, false, true);
    }

    public void showUnitsNextToStructure(UnitsType type, bool isPoweredUnit) 
    {
        WireProtectorSlotClicks();
        if (type == UnitsType.Infantry)
        {
            if (isPoweredUnit) {
                unitsProtectorSlots[5].gameObject.SetActive(true);
            }
            else
            {
                unitsProtectorSlots[0].gameObject.SetActive(true);
            }
        } else if (type == UnitsType.Archer) {
            if (isPoweredUnit)
            {
                unitsProtectorSlots[8].gameObject.SetActive(true);
            }
            else
            {
                unitsProtectorSlots[3].gameObject.SetActive(true);

            }
            
        } else if (type == UnitsType.Mortar) {
            if (isPoweredUnit)
            {
                unitsProtectorSlots[6].gameObject.SetActive(true);
            }
            else
            {
                unitsProtectorSlots[3].gameObject.SetActive(true);

            }
            
        } else if (type == UnitsType.AntiBlindage) {
            if (isPoweredUnit)
            {
                unitsProtectorSlots[9].gameObject.SetActive(true);
            }
            else
            {
                unitsProtectorSlots[4].gameObject.SetActive(true);

            }

        } else if (type == UnitsType.Heavy) {
            if (isPoweredUnit)
            {
                unitsProtectorSlots[7].gameObject.SetActive(true);
            }
            else
            {
                unitsProtectorSlots[2].gameObject.SetActive(true);

            }
        }
    }
}