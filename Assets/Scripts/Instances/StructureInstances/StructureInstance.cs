using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Ce script doit être rattaché à un GO directement dans la scène 
// et il doit faire référence à l'unitsManager, pour pouvoir utiliser la fonction SpawnUnitByTypeAtPosition

public class StructureInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StructureData structureData;
    public UnitsManager unitsManager; //Permet d'appeler l'unitsManager
    public GameObject structureInterface;

    private Vector3 structurePosition; // Récupère la position de la structure dans la scène
    private Queue<UnitData> unitQueue = new Queue<UnitData>(); // File d'attente pour les unités à créer
    private bool isSpawning = false; // Indique si la structure est actuellement en train de créer des unités
    public bool neutralStructure;
    public StructureType structureType;

    [Header("Units")]
    public List<UnitsType> unitsProtectorTypes = new List<UnitsType>();
    public int UnitsProtector;
    public bool isAlive;

    [Header("Player")]
    public PlayerNumber player;

    private Outline outline; // Quick Outline composant

    void Awake()
    {
        outline = GetComponent<Outline>(); // Récupère le composant Quick Outline
        if (outline != null)
            outline.enabled = false; // contour désactivé au départ
    }

    void Start()
    {
        structurePosition = transform.position; // Initialise la position au démarrage
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        UnSelected();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddToQueue(UnitsType.Infantry);
        }

        if (!isSpawning && unitQueue.Count > 0)
        {
            StartCoroutine(SpawnUnitsWithDelay(structurePosition));
        }
    }

    public void AddToQueue(UnitsType type)
    {
        UnitData data = unitsManager.unitData.Find(d => d.type == type);
        if (data != null)
        {
            unitQueue.Enqueue(data);
        }
    }

    public void OnMouseDown()
    {
        Selected();
    }

    void Selected()
    {
        if (structureInterface != null)
        {
            Debug.Log("Selected structure: " + gameObject.name);
            structureInterface.SetActive(true);
            structureInterface.transform.SetAsLastSibling();
            PositionInterface();
        }
        if (outline != null)
            outline.enabled = true;
    }

    void PositionInterface()
    {
        if (structureInterface == null)
            return;

        RectTransform rectTransform = structureInterface.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Définir la taille
            rectTransform.sizeDelta = new Vector2(1920, 1080);

            // Ancrer en bas à gauche du Canvas
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            
            // Le pivot en bas à gauche de l'interface elle-même
            rectTransform.pivot = new Vector2(0, 0);

            // Positionner le coin inférieur gauche exactement au coin inférieur gauche de l'écran
            rectTransform.anchoredPosition = Vector2.zero;
            
            // S'assurer que la position locale est également à zéro
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0);
        }
    }

    public void UnSelected()
    {
        // Cache le menu UI
        if (structureInterface != null)
            structureInterface.SetActive(false);

        // Restaure la couleur du bâtiment
        GetComponent<Renderer>().material.color = Color.white;

        // Désactive le contour Quick Outline
        if (outline != null)
            outline.enabled = false;
    }

    private IEnumerator SpawnUnitsWithDelay(Vector3 position)
    {
        isSpawning = true;
        while (unitQueue.Count > 0)
        {
            UnitData data = unitQueue.Dequeue();
            unitsManager.SpawnUnitByTypeAtPosition(data.type, position);
            yield return new WaitForSeconds(data.creationTime);
        }
        isSpawning = false;
    }
}