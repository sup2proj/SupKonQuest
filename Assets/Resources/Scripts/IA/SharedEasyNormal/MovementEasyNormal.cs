using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(UnitInstance))]
public class MovementEasyNormal : MonoBehaviour
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
    private NormalAttack normalAttackComponent;

    private void Awake()
    {
        unitInstance = GetComponent<UnitInstance>();
        movementManager = GetComponent<MovementManager>();
        unitsAnimation = GetComponent<UnitsAnimation>();
        normalAttackComponent = GetComponent<NormalAttack>();
        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();
    }

    private void OnEnable()
    {
        ScheduleNextMove(0f);

        int difficulty = 1;
        if (unitInstance != null && IAInstance.TryGetAIForPlayer(unitInstance.playerId, out IAInstance iaInstance) && iaInstance != null)
            difficulty = iaInstance.DifficultyIA;

        if (difficulty == 2)
        {
            if (normalAttackComponent == null)
                normalAttackComponent = gameObject.AddComponent<NormalAttack>();

            if (normalAttackComponent != null && unitInstance != null)
            {
                normalAttackComponent.SetOwnerPlayerId(unitInstance.playerId);
                Debug.Log($"[MovementEasyNormal] {gameObject.name} NormalAttack added/configured for owner={unitInstance.playerId}");
            }
        }

        if (movementManager != null && !movementManager.IsMoving())
        {
            StartRandomMove();
        }
    }

    private void Update()
    {
        if (unitInstance == null || movementManager == null || unitInstance.unitData == null)
            return;

        UpdateCombatStatus();

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
        bool isCurrentlyInCombat = false;
        Transform currentAttackTarget = null;

        if (unitsAnimation != null)
        {
            currentAttackTarget = GetAttackTargetFromAnimation();
            isCurrentlyInCombat = currentAttackTarget != null;
        }

        if (!isInCombat && isCurrentlyInCombat)
        {
            isInCombat = true;
            movementManager.StopMovement();
            lastAttackTarget = currentAttackTarget;
        }
        else if (isInCombat && !isCurrentlyInCombat)
        {
            if (lastAttackTarget != null)
            {
                UnitInstance targetUnit = lastAttackTarget.GetComponent<UnitInstance>();
                StructureInstance targetStructure = lastAttackTarget.GetComponent<StructureInstance>();
                if ((targetUnit != null && targetUnit.currentHealth <= 0) || (targetStructure != null && targetStructure.currentHealth <= 0))
                {
                    isInCombat = false;
                    lastAttackTarget = null;
                    ScheduleNextMove(0f);
                }
            }
            else
            {
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
            return;
        }

        if (TryGetLocalFallbackDestination(out Vector3 fallback))
        {
            float moveDuration = Random.Range(moveDurationMin, moveDurationMax);
            nextStateChangeTime = Time.time + moveDuration;
            movementManager.ForceMoveToPosition(fallback, stopDistance);
            return;
        }

        ScheduleNextMove(1f);
    }

    private bool TryGetLocalFallbackDestination(out Vector3 destination)
    {
        destination = transform.position;

        for (int i = 0; i < 8; i++)
        {
            Vector3 cand = transform.position + (Vector3)(Random.insideUnitCircle * 3f);
            cand.y = transform.position.y;
            if (movementManager.CanMoveOnWorldPosition(cand))
            {
                destination = cand;
                return true;
            }
        }

        return false;
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
