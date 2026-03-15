using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class InterfaceInstance : MonoBehaviour
{
    public static InterfaceInstance Instance { get; private set; }

    [Header("UI")]
    [SerializeField] public GameObject unitsQueue;

    [Header("Queue item")]
    [SerializeField] private Vector3 queuedItemLocalScale = new Vector3(0.75f, 0.35f, 1f);
    [SerializeField] private Image[] queueSlots;
    
    [Header("Progression Bar")]
    [SerializeField] public ProgressBar progressBar;
    
    [Header("Runtime")]
    public static float currentProgression;
    
    private int slotAvailabel = -1;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (unitsQueue != null)
        {
            unitsQueue.SetActive(false);
        }
        if (unitsQueue == null)
        {
            Debug.LogWarning("[InterfaceInstance] unitsQueue n'est pas assigné dans l'Inspector.");
            return;
        }
    }

    void Update()
    {

    }

    public void ShowUnitsQueue()
    {
            unitsQueue.SetActive(true);
    }

    public void HideUnitsQueue()
    {
        unitsQueue.SetActive(false);
    }

    public void addUnitToQueue(GameObject clickedUnit)
    {
        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (queueSlots[i].sprite == null)
            {
                FillSlotImage(i, clickedUnit);
                slotAvailabel = i + 1;
                
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
    
    public void InitUnitsCreation(int unitIndex)
    {
        if (progressBar == null)
        {
            Debug.LogWarning($"[UnitInstance] {name} : progressBar non assignée dans l'inspector.", this);
            return;
        }

        progressBar.StartCreation(ActionInterface.Instance.unitDatas[unitIndex].creationTime);
        StartCoroutine(WaitProgressBarFinished());

        progressBar.transform.localPosition = (1.1f * Vector3.up);
    }

    private IEnumerator WaitProgressBarFinished()
    {
        yield return null;

        while (progressBar != null && !progressBar.IsFinished())
            yield return null;

        // if (progressBar != null)
        //     //appelle du manager pour créer l'unité 
    }



}
