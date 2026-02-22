using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Ce script doit être rattaché à un GO directement dans la scène 
// et il doit faire référence à l'unitsManager, pour pouvoir utiliser la fonction SpawnUnitByTypeAtPosition


public class StructureInstance : MonoBehaviour
{
    
    public UnitsManager unitsManager; //Permet d'appeler l'unitsManager
    
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
    
    void Start()
    {
        structurePosition = transform.position; // Initialise la position au démarrage
    }
    
    // L'update permet de vérifier si le joueur appuie sur la touche espace pour ajouter une unité à la file d'attente,
    // et si la structure n'est pas déjà en train de créer des unités et qu'il y a des unités dans la file d'attente,
    // elle lance la coroutine pour créer les unités avec un délai entre chaque création
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

    // Méthode pour ajouter une unité à la file d'attente, elle prend en paramètre le type d'unité à créer,
    // elle recherche les données correspondantes dans la liste unitsData de l'unitsManager et les ajoute à la file d'attente
    public void AddToQueue(UnitsType type)
    {
        UnitsData data = unitsManager.unitsData.Find(d => d.type == type);
        if (data != null)
        {
            unitQueue.Enqueue(data);
        }
    }

    // Coroutine pour créer les unités avec un délai entre chaque création, la coroutine permet d'avoir un temps, c'est un 
    // peu comme dans un projet javascript avec asynchronous, on peut faire une pause dans l'exécution du code pour attendre un certain temps avant de continuer,
    // ici on attend le temps de création de chaque unité avant de créer la suivante
    private IEnumerator SpawnUnitsWithDelay(Vector3 position)
    {
        isSpawning = true;
        while (unitQueue.Count > 0)
        {
            UnitsData data = unitQueue.Dequeue();
            unitsManager.SpawnUnitByTypeAtPosition(data.type, position);

            yield return new WaitForSeconds(data.creationTime); // Attendre avant de créer la prochaine unité
        }

        isSpawning = false;
    }
}