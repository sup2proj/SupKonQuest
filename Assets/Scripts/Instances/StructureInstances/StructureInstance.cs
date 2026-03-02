using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Ce script doit être rattaché à un GO directement dans la scène 
// et il doit faire référence à l'unitsManager, pour pouvoir utiliser la fonction SpawnUnitByTypeAtPosition

public class StructureInstance : MonoBehaviour
{
    public UnitsManager unitsManager; //Permet d'appeler l'unitsManager
    public GameObject structureInterface;

    private Vector3 structurePosition; // Récupère la position de la structure dans la scène
    private Queue<UnitsData> unitQueue = new Queue<UnitsData>(); // File d'attente pour les unités à créer
    private bool isSpawning = false; // Indique si la structure est actuellement en train de créer des unités
    public bool neutralStructure;
    public StructureType structureType;

    [Header("Units")]
    public List<UnitsType> unitsProtectorTypes = new List<UnitsType>();
    public int UnitsProtector;
    public bool isAlive;

    [Header("Player")]
    public PlayerNumber player;

    private bool isSelected = false;
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
        UnitsData data = unitsManager.unitsData.Find(d => d.type == type);
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
        isSelected = true;
        if (structureInterface != null)
        {
            structureInterface.SetActive(true);
            structureInterface.transform.SetAsLastSibling();
        }
        if (outline != null)
            outline.enabled = true;
    }

    public void UnSelected()
    {
        isSelected = false;

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
            UnitsData data = unitQueue.Dequeue();
            unitsManager.SpawnUnitByTypeAtPosition(data.type, position);

            yield return new WaitForSeconds(data.creationTime);
        }
        isSpawning = false;
    }
}