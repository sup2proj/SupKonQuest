using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(UnitInstance))]
public class EasyMovement : MonoBehaviour
{
    [Header("Easy Movement")]
    [SerializeField, Min(1f)] private float moveDurationMin = 2f;
    [SerializeField, Min(1f)] private float moveDurationMax = 6f;
    [SerializeField, Min(0f)] private float pauseDurationMin = 5f;
    [SerializeField, Min(0f)] private float pauseDurationMax = 10f;
    [SerializeField, Min(0.1f)] private float stopDistance = 0.5f;

    [Header("Références")]
    [SerializeField] private MapGenerator mapGenerator;

    private UnitInstance unitInstance;
    private MovementManager movementManager;
    private UnitsAnimation unitsAnimation;
    private float nextStateChangeTime;
    private bool isPaused = false;
    private bool isInCombat = false;
    private Transform lastAttackTarget = null;

    private void Awake()
    {
        unitInstance = GetComponent<UnitInstance>();
        movementManager = GetComponent<MovementManager>();
        unitsAnimation = GetComponent<UnitsAnimation>();
        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();
    }

    private void OnEnable()
    {
        ScheduleNextMove(0f);
    }

    private void Update()
    {
        if (unitInstance == null || movementManager == null || unitInstance.unitData == null)
            return;
        UpdateCombatStatus();
        // Si l'unité est en combat, on suspend le cycle de mouvement aléatoire
        if (isInCombat)
            return;

        if (isPaused)
        {
            if (Time.time >= nextStateChangeTime)
            {
                isPaused = false;
                StartRandomMove();
            }
            return;
        }

        if (!movementManager.IsMoving())
        {
            if (Time.time >= nextStateChangeTime)
            {
                isPaused = true;
                SchedulePause();
            }
            return;
        }

        if (Time.time >= nextStateChangeTime)
        {
            movementManager.StopMovement();
            isPaused = true;
            SchedulePause();
        }
    }

    private void UpdateCombatStatus()
    {
        // Vérifier si l'unité a un ennemi à attaquer
        bool isCurrentlyInCombat = false;
        Transform currentAttackTarget = null;

        if (unitsAnimation != null)
        {
            // On vérifie si UnitsAnimation a un attackTarget via réflexion ou via une méthode publique
            // Pour l'instant, on utilise la propriété d'accès qu'on peut créer
            currentAttackTarget = GetAttackTargetFromAnimation();
            isCurrentlyInCombat = currentAttackTarget != null;
        }

        // Si on entre en combat (transition de non-combat à combat)
        if (!isInCombat && isCurrentlyInCombat)
        {
            Debug.Log($"[EasyMovement] {gameObject.name} entre en combat contre {currentAttackTarget.name}");
            isInCombat = true;
            movementManager.StopMovement();
            lastAttackTarget = currentAttackTarget;
        }
        // Si on quitte le combat
        else if (isInCombat && !isCurrentlyInCombat)
        {
            // Vérifier si la cible est toujours vivante
            if (lastAttackTarget != null)
            {
                UnitInstance targetUnit = lastAttackTarget.GetComponent<UnitInstance>();
                StructureInstance targetStructure = lastAttackTarget.GetComponent<StructureInstance>();

                // Si la cible est morte, on sort du combat
                if ((targetUnit != null && targetUnit.currentHealth <= 0) || (targetStructure != null && targetStructure.currentHealth <= 0))
                {
                    Debug.Log($"[EasyMovement] {gameObject.name} sort du combat (cible morte)");
                    isInCombat = false;
                    lastAttackTarget = null;
                    // Relancer le cycle de mouvement aléatoire
                    ScheduleNextMove(0f);
                }
            }
            else
            {
                // Si plus de cible, on sort du combat
                Debug.Log($"[EasyMovement] {gameObject.name} sort du combat (plus de cible)");
                isInCombat = false;
                lastAttackTarget = null;
                ScheduleNextMove(0f);
            }
        }
    }

    private Transform GetAttackTargetFromAnimation()
    {
        if (unitsAnimation == null)
            return null;
    
        return unitsAnimation.AttackTarget;
    }
    
    private void SchedulePause()
    {
        float pause = Random.Range(pauseDurationMin, pauseDurationMax);
        nextStateChangeTime = Time.time + pause;
    }

    private void ScheduleNextMove(float immediateDelay)
    {
        isPaused = false;
        nextStateChangeTime = Time.time + Mathf.Max(0f, immediateDelay);
    }

    private void StartRandomMove()
    {
        if (TryGetRandomDestination(out Vector3 destination))
        {
            float moveDuration = Random.Range(moveDurationMin, moveDurationMax);
            nextStateChangeTime = Time.time + moveDuration;
            movementManager.MoveToPosition(destination, stopDistance);
        }
        else
        {
            ScheduleNextMove(1f);
        }
    }

    private bool TryGetRandomDestination(out Vector3 destination)
    {
        destination = transform.position;
        if (mapGenerator == null || mapGenerator.allTiles == null)
            return false;

        int width = mapGenerator.allTiles.GetLength(0);
        int height = mapGenerator.allTiles.GetLength(1);
        if (width == 0 || height == 0)
            return false;

        int currentX = Mathf.Clamp(Mathf.RoundToInt(transform.position.x / mapGenerator.tileSize), 0, width - 1);
        int currentY = Mathf.Clamp(Mathf.RoundToInt(transform.position.z / mapGenerator.tileSize), 0, height - 1);

        for (int attempt = 0; attempt < 24; attempt++)
        {
            int tx = Random.Range(0, width);
            int ty = Random.Range(0, height);
            if (Mathf.Abs(tx - currentX) + Mathf.Abs(ty - currentY) < 4)
                continue;

            TileData tile = mapGenerator.allTiles[tx, ty];
            if (tile == null)
                continue;

            Vector3 candidate = new Vector3(tx * mapGenerator.tileSize, transform.position.y, ty * mapGenerator.tileSize);
            if (!movementManager.CanMoveOnWorldPosition(candidate))
                continue;

            destination = candidate;
            return true;
        }

        return false;
    }
}
