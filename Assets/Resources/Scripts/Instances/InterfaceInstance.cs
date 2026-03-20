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

    private static readonly Dictionary<int, UnitsType> ProtectorSlotToType = new Dictionary<int, UnitsType>
    {
        { 0, UnitsType.Infantry },
        { 1, UnitsType.Mortar },
        { 2, UnitsType.Heavy },
        { 3, UnitsType.Archer },
        { 4, UnitsType.AntiBlindage },
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
    
    public void InitUnitsCreation(int unitIndex, UnitsType type, float x, float z, bool isPoweredUnit)
    {
        creationQueue.Enqueue(new UnitCreationRequest
        {
            unitIndex = unitIndex,
            type = type,
            x = x,
            z = z,
            isPoweredUnit = isPoweredUnit
        });

        if (!isSpawning)
            StartCoroutine(ProcessCreationQueue());
    }

    private IEnumerator ProcessCreationQueue()
    {
        isSpawning = true;

        while (creationQueue.Count > 0)
        {
            UnitCreationRequest req = creationQueue.Dequeue();

            var actionInterface = ActionInterface.Instance;
            if (actionInterface == null || actionInterface.unitDatas == null || req.unitIndex < 0 || req.unitIndex >= actionInterface.unitDatas.Length)
            {
                Debug.LogWarning($"[InterfaceInstance] Impossible de lancer la creation: unitDatas invalide (index={req.unitIndex}).", this);
                ShiftQueueLeft();
                continue;
            }

            float creationTime = actionInterface.unitDatas[req.unitIndex].creationTime;

            // La barre est purement visuelle: la production suit son propre timer.
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

            StructureManager.Instance.SpawnUnitByTypeAtPosition(req.type, req.x, req.z, req.isPoweredUnit, false);

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
        StructureManager.Instance.SpawnUnitByTypeAtPosition(type, pos.x + 1f, pos.z + 1f, false, true);
    }

    public void showUnitsNextToStructure(UnitsType type) 
    {
        WireProtectorSlotClicks();
        if (type == UnitsType.Infantry)
        {
            unitsProtectorSlots[0].gameObject.SetActive(true);
        } else if (type == UnitsType.Archer)
        {
            unitsProtectorSlots[3].gameObject.SetActive(true);
            
        } else if (type == UnitsType.Mortar)
        {
            unitsProtectorSlots[1].gameObject.SetActive(true);
            
        } else if (type == UnitsType.AntiBlindage)
        {
            unitsProtectorSlots[4].gameObject.SetActive(true);

        } else if (type == UnitsType.Heavy)
        {
            unitsProtectorSlots[2].gameObject.SetActive(true);
        }
    }
}
