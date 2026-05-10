using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;


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
    
	[Header("Player Statistics")]
	[SerializeField] private StatisticsInterface statisticsInterface;
    
    [Header("Buff (support/healer)")]
    [SerializeField] private Image[] buffSlots;
    
    [Header("Boat exit button")]
    [SerializeField] private Image boatExitButton;
    

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
        public int playerId;
        public int paidCost;
        public int buildingPlayerId;
        public int sourceStructureId;
    }
    
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
        HideProgressBarVisual(resetProgress: false);
        hideBuffIcons();
        HideBoatExitIcons();
        WireBoatExitButtonClick();
        WireBuffSlotClicks();
        WirePlayersListClicks();
        WireProtectorSlotClicks();
        RefreshPlayerStatisticsUI();
    }

    void Update()
    {
    }

    public void showInterfaceForStructure()
    {
        unitsQueue.SetActive(true);
        unitsProtector.SetActive(true);
        hideUnitsProtectorSlots();
        displayedStructure = StructureInstance.CurrentlySelected;
        RefreshQueueSlotsVisibility();
        RefreshProgressBarVisibility();
    }

    public void HideStructureInterface()
    {
        unitsQueue.SetActive(false);
        unitsProtector.SetActive(false);
        SetAllQueueSlotsActive(false);
        HideProgressBarVisual(resetProgress: false);
    }

    public void addUnitToQueue(GameObject clickedUnit)
    {
        int structureId = GetCurrentStructureId();
        if (structureId == -1 || clickedUnit == null)
            return;

        var source = clickedUnit.GetComponentInChildren<Image>(true);
        if (source == null || source.sprite == null)
            return;

        var queue = GetOrCreateStructureQueue(structureId);
        if (queueSlots == null)
            return;
        if (queue.Count >= queueSlots.Length)
            return;

        queue.Add(source.sprite);
        RefreshQueueSlotsVisibility();
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
    
    public bool InitUnitsCreation(int unitIndex, UnitsType type, float x, float z, bool isPoweredUnit, bool isProtector)
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
            RefreshPlayerStatisticsUI();
            return false;
        }

        Debug.Log($"[InterfaceInstance] Création demandée et payée: joueur={playerId}, coût={cost}, goldRestant={session.Gold}, type={type}, powered={isPoweredUnit}.", this);
        
        int buildingPlayerId = playerId;
        var selectedStructure = StructureInstance.CurrentlySelected;
        int sourceStructureId = -1;
        if (selectedStructure != null)
        {
            buildingPlayerId = selectedStructure.playerId;
            sourceStructureId = selectedStructure.GetInstanceID();
        }
        
        var creationState = GetOrCreateCreationState(sourceStructureId);
        creationState.queue.Enqueue(new UnitCreationRequest
        {
            unitIndex = unitIndex,
            type = type,
            x = x,
            z = z,
            isPoweredUnit = isPoweredUnit,
            isProtector = isProtector,
            playerId = playerId,
            paidCost = cost,
            buildingPlayerId = buildingPlayerId,
            sourceStructureId = sourceStructureId,
        });

        if (creationState.coroutine == null)
            creationState.coroutine = StartCoroutine(ProcessCreationQueue(sourceStructureId));

        RefreshPlayerStatisticsUI();
        return true;
    }

    private void RefreshPlayerStatisticsUI()
    {
        if (statisticsInterface != null)
            statisticsInterface.Refresh();
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

        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        if (playerManager != null)
            playerManager.SetActivePlayer(playerId);

        int gold = -1;
        if (playerManager != null)
        {
            var session = playerManager.GetSession(playerId);
            gold = session != null ? session.Gold : -1;
        }

        Debug.Log($"[InterfaceInstance] Switch joueur (UI): uiIndex={uiIndex} => playerId={playerId}, gold={gold}.", this);

        RefreshPlayerStatisticsUI();
    }

    private int GetSelectedPlayerId()
    {
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

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
    // TEMPORAIRE -----------------------------------------------------------------------------

    private IEnumerator ProcessCreationQueue(int structureId)
    {
        if (structureId == -1)
            yield break;

        var creationState = GetOrCreateCreationState(structureId);
        while (creationState.queue.Count > 0)
        {
            UnitCreationRequest req = creationState.queue.Dequeue();

            var actionInterface = ActionInterface.Instance;
            float creationTime = actionInterface.unitDatas[req.unitIndex].creationTime;
            creationState.hasCurrentRequest = true;
            creationState.currentCreationStartedAt = Time.time;
            creationState.currentCreationDuration = creationTime;

            if (progressBar != null)
            {
                RefreshProgressBarVisibility();
                if (progressBarBoundStructureId == structureId)
                {
                    progressBar.StartCreation(creationTime);
                    progressBar.transform.localPosition = (1.1f * Vector3.up);
                }
            }

            float elapsed = 0f;
            while (elapsed < creationTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (progressBar != null)
            {
                if (progressBarBoundStructureId == structureId)
                    HideProgressBarVisual(resetProgress: true);
            }

            if (StructureManager.Instance == null)
            {
                Debug.LogError($"[InterfaceInstance] StructureManager.Instance est null -> spawn annulé. playerId={req.buildingPlayerId}, type={req.type}", this);
            }
            else
            {
                StructureInstance sourceStructure = StructureInstance.FindByInstanceId(req.sourceStructureId);
                bool spawned = StructureManager.Instance.SpawnUnitByTypeAtPosition(
                    req.buildingPlayerId,
                    req.type,
                    req.x,
                    req.z,
                    req.isPoweredUnit,
                    req.isProtector,
                    sourceStructure
                );
            }

            ShiftQueueLeft(req.sourceStructureId);
            creationState.hasCurrentRequest = false;
            creationState.currentCreationDuration = 0f;
        }

        creationState.coroutine = null;
        creationState.hasCurrentRequest = false;
        creationState.currentCreationDuration = 0f;
        if (progressBarBoundStructureId == structureId)
            HideProgressBarVisual(resetProgress: true);
    }

    private void ShiftQueueLeft(int structureId)
    {
        if (structureId == -1)
            return;

        var queue = GetOrCreateStructureQueue(structureId);
        if (queue.Count > 0)
            queue.RemoveAt(0);
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
        int structureId = GetCurrentStructureId();
        List<Sprite> queue = null;
        if (structureId != -1)
            queuedSpritesByStructure.TryGetValue(structureId, out queue);

        for (int i = 0; i < queueSlots.Length; i++)
        {
            var img = queueSlots[i];
            if (img == null) continue;
            Sprite sprite = (queue != null && i < queue.Count) ? queue[i] : null;
            img.sprite = sprite;
            bool hasSprite = sprite != null;
            img.gameObject.SetActive(hasSprite);
        }
    }

    private int GetCurrentStructureId()
    {
        if (displayedStructure == null)
            displayedStructure = StructureInstance.CurrentlySelected;
        return displayedStructure != null ? displayedStructure.GetInstanceID() : -1;
    }

    private List<Sprite> GetOrCreateStructureQueue(int structureId)
    {
        if (!queuedSpritesByStructure.TryGetValue(structureId, out var queue))
        {
            queue = new List<Sprite>();
            queuedSpritesByStructure[structureId] = queue;
        }
        return queue;
    }

    private BuildingCreationState GetOrCreateCreationState(int structureId)
    {
        if (!creationStateByStructure.TryGetValue(structureId, out var state))
        {
            state = new BuildingCreationState();
            creationStateByStructure[structureId] = state;
        }
        return state;
    }

    private void RefreshProgressBarVisibility()
    {
        if (progressBar == null)
            return;

        int displayedStructureId = GetCurrentStructureId();
        progressBarBoundStructureId = displayedStructureId;
        bool shouldShow = false;
        if (displayedStructureId != -1 && creationStateByStructure.TryGetValue(displayedStructureId, out var state) && state.hasCurrentRequest)
        {
            float elapsed = Time.time - state.currentCreationStartedAt;
            progressBar.StartCreationFromElapsed(state.currentCreationDuration, elapsed);
            shouldShow = true;
        }
        progressBar.SetFillVisible(shouldShow);
    }

    private void HideProgressBarVisual(bool resetProgress)
    {
        if (progressBar == null)
            return;

        if (resetProgress)
            progressBar.StopCreation(resetToZero: true);
        progressBar.SetFillVisible(false);
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

    private void WireBoatExitButtonClick()
    {
        if (boatExitButton == null)
            return;

        var btn = boatExitButton.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnBoatExitClicked());
    }
    
    private void OnBoatExitClicked()
    {
        int activePlayerId = PlayerManager.Instance != null ? PlayerManager.Instance.GetActivePlayerId() : GetSelectedPlayerId();
        int unloadedBoats = 0;

        if (SelectionManager.Instance != null && SelectionManager.Instance.CurrentlySelectedObjects != null)
        {
            for (int i = 0; i < SelectionManager.Instance.CurrentlySelectedObjects.Count; i++)
            {
                SelectableObject selectable = SelectionManager.Instance.CurrentlySelectedObjects[i];
                if (selectable == null)
                    continue;

                UnitInstance unit = selectable.GetComponent<UnitInstance>();
                if (unit == null || unit.playerId != activePlayerId)
                    continue;

                if (BoatTransport.ExitAllUnits(unit))
                    unloadedBoats++;
            }
        }

        Debug.Log($"[InterfaceInstance] BoatExit: {unloadedBoats} bateau(x) ont debarque leurs unites.");
    }

    private void WireBuffSlotClicks()
    {
        if (buffSlots == null || buffSlots.Length == 0)
            return;

        for (int i = 0; i < buffSlots.Length; i++)
        {
            var img = buffSlots[i];
            if (img == null) continue;

            var btn = img.GetComponent<Button>();
            int capturedIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnBuffSlotClicked(capturedIndex));
        }
    }

    private void OnBuffSlotClicked(int index)
    {
        if (buffSlots == null || index < 0 || index >= buffSlots.Length || buffSlots[index] == null)
            return;
        Spells.Instance.ButtonListener(index);
        Debug.Log($"[InterfaceInstance] Bouton buff appuyé: index={index}, nom={buffSlots[index].gameObject.name}", buffSlots[index]);
    }

    private void SpawnStructProtectorOnInterface(int slotIndex)
    {
        ProtectorSlotToType.TryGetValue(slotIndex, out var type);
        var selected = StructureInstance.CurrentlySelected;
        Vector3 pos = selected.StructurePosition;
        int buildingPlayerId = selected != null ? selected.playerId : GetSelectedPlayerId();
        bool accepted = InitUnitsCreation(slotIndex, type, pos.x + 1f, pos.z + 1f, false, true);
        // Modification de slotIndex car dans les datas les unités ne sont pas dans le bonne ordre
        if (slotIndex == 0) {
            slotIndex = 5;
        } else if (slotIndex == 1) {
            slotIndex = 6;
        } else if (slotIndex == 2) {
            slotIndex = 4;
        } else if (slotIndex == 3) {
            slotIndex = 2;
        } else if (slotIndex == 4) {
            slotIndex = 1;
        }
        GameObject clickedImageGO = ActionInterface.Instance.GetClickedUnitsIcon(slotIndex, false);
        if (accepted && clickedImageGO != null)
        {
            addUnitToQueue(clickedImageGO);
        }
    }

    public void showUnitsNextToStructure(UnitsType type, bool isPoweredUnit) 
    {
        WireProtectorSlotClicks();
        if (type == UnitsType.Infantry)
        {
            unitsProtectorSlots[0].gameObject.SetActive(true);
            ActionInterface.Instance.ShowUnitProtectorPrice(5, 4);
        } else if (type == UnitsType.Mortar) {
            unitsProtectorSlots[1].gameObject.SetActive(true);
            ActionInterface.Instance.ShowUnitProtectorPrice(6, 3);
        } else if (type == UnitsType.Heavy) {
            unitsProtectorSlots[2].gameObject.SetActive(true);
            ActionInterface.Instance.ShowUnitProtectorPrice(4, 2);
        } else if (type == UnitsType.Archer) {
            unitsProtectorSlots[3].gameObject.SetActive(true);
            ActionInterface.Instance.ShowUnitProtectorPrice(1, 1);
        }  else if (type == UnitsType.AntiBlindage) {
            unitsProtectorSlots[4].gameObject.SetActive(true);
            ActionInterface.Instance.ShowUnitProtectorPrice(0, 0);
        } 
    }

    public void hideBuffIcons()
    {
        if (buffSlots == null)
            return;

        foreach (var buffIcon in buffSlots)
        {
            if (buffIcon == null) continue;
            buffIcon.gameObject.SetActive(false);
        }
    }

    public void ShowSupportIcons()
    {
        int slotsToShow = Mathf.Min(buffSlots.Length, 3);
        for (int i = 0; i < slotsToShow; i++)
        {
            if (buffSlots[i] == null) continue;
            buffSlots[i].gameObject.SetActive(true);
        }
    }
    
    public void ShowHealerIcon()
    {
        int healerIndex = buffSlots.Length - 1;
        if (buffSlots[healerIndex] == null)
            return;
    
        buffSlots[healerIndex].gameObject.SetActive(true);
    }

    public void ShowBoatExitIcons()
    {
        boatExitButton.gameObject.SetActive(true);
    }
    
    public void HideBoatExitIcons()
    {
        boatExitButton.gameObject.SetActive(false);
    }
    
    public void HideBuffIconForCooldown(int slotIndex, float cooldown)
    {
        Image slot = buffSlots[slotIndex];
        if (slot == null)
            return;
        if (buffSlotReappearCoroutines.TryGetValue(slotIndex, out var existing) && existing != null)
            StopCoroutine(existing);
        slot.gameObject.SetActive(false);

        if (cooldown <= 0f)
        {
            slot.gameObject.SetActive(true);
            buffSlotReappearCoroutines.Remove(slotIndex);
            return;
        }

        buffSlotReappearCoroutines[slotIndex] = StartCoroutine(ShowBuffIconAfterDelay(slotIndex, cooldown));
    }

    private IEnumerator ShowBuffIconAfterDelay(int slotIndex, float cooldown)
    {
        yield return new WaitForSeconds(cooldown);

        if (buffSlots != null && slotIndex >= 0 && slotIndex < buffSlots.Length && buffSlots[slotIndex] != null)
            buffSlots[slotIndex].gameObject.SetActive(true);

        buffSlotReappearCoroutines.Remove(slotIndex);
    }
}