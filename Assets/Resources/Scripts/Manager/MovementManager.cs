using Enums.Environment;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using System.Collections.Generic;

public class MovementManager : MonoBehaviour
{
    public static MovementManager Instance;
    
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0.3f;
    public float rotationSpeed = 12f;
	
    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private Transform followTarget;
    private float stuckTimer = 0f;

    private UnitInstance unitInstance;
    private NavMeshAgent agent;
    private Animator animator;
    private SelectableObject selectable;
    private MapGenerator mapGenerator;

    private static int priorityCounter = 0;

    private void Awake()
    {
        Instance = this;
        unitInstance = GetComponent<UnitInstance>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        selectable = GetComponent<SelectableObject>();

        if (agent != null)
        {
            agent.speed = GetUnitSpeed();
            agent.areaMask = GetAllowedNavMeshAreaMask();
            agent.avoidancePriority = Mathf.Clamp(priorityCounter % 99, 1, 99);
            priorityCounter++;
        }
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.speed = GetUnitSpeed();
            agent.areaMask = GetAllowedNavMeshAreaMask();
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, agent.areaMask))
            {
                agent.Warp(hit.position);
            }
        }
    }

    private void Update()
    {
        if (SelectionManager.Instance == null)
            HandleMouseClick();
        HandleMovement();
    }

    private void HandleMouseClick()
    {
        if (Mouse.current == null || Camera.main == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 destination = hit.point;
                destination.y = transform.position.y;
                MoveToPosition(destination, stoppingDistance);
            }
        }
    }

    private void HandleMovement()
    {
        if (agent != null)
        {
            agent.speed = GetUnitSpeed();
            agent.areaMask = GetAllowedNavMeshAreaMask();
        }

        // Version optimisée utilisant NavMeshAgent
        if (CanUseNavMeshAgent())
        {
            HandleNavMeshMovement();
        }
        else
        {
            HandleTransformMovement();
        }

        // Mise à jour de l'animation
        UpdateMovementAnimation();
    }

    private void HandleNavMeshMovement()
    {
        if (isMovingToTarget)
        {
            
            // Si on suit une cible, mettre à jour la destination
            if (followTarget != null)
            {
                agent.SetDestination(followTarget.position);
            }

            float dynamicStoppingDistance = Mathf.Max(agent.stoppingDistance, agent.speed * 0.1f);
            //Debug.Log($"Agent stopping distance: {agent.stoppingDistance:F3} | Speed: {agent.speed:F3}");
            //Debug.Log($"Distance restante: {agent.remainingDistance:F3} | StoppingDistance: {dynamicStoppingDistance:F3} | Manque: {(agent.remainingDistance - dynamicStoppingDistance):F3}");

            if (!agent.pathPending
                && agent.remainingDistance <= dynamicStoppingDistance
                && agent.velocity.sqrMagnitude < 0.01f)
            {
                isMovingToTarget = false;
                agent.ResetPath();
                stuckTimer = 0f;
                NotifyMovementComplete();
            }
            else if (agent.velocity.magnitude < 0.05f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 1.5f)
                {
                    isMovingToTarget = false;
                    agent.velocity = Vector3.zero;
                    agent.ResetPath();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
    }

    private void HandleTransformMovement()
    {
        if (!isMovingToTarget)
        {
            movement = Vector3.zero;
            return;
        }

        if (followTarget != null)
            targetPosition = followTarget.position;

        Vector3 current = transform.position;
        Vector3 toTarget = targetPosition - current;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance <= stoppingDistance)
        {
            isMovingToTarget = false;
            movement = Vector3.zero;
            NotifyMovementComplete();
            return;
        }

        Vector3 desiredDir = toTarget.normalized;
        movement = desiredDir;

        float finalSpeed = GetUnitSpeed();
        transform.position += movement * finalSpeed * Time.deltaTime;

        Quaternion targetRot = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private void UpdateMovementAnimation()
    {
        if (animator != null)
        {
            bool isActuallyMoving = false;
            if (CanUseNavMeshAgent())
                isActuallyMoving = isMovingToTarget && agent.velocity.magnitude > 0.1f;
            else
                isActuallyMoving = isMovingToTarget && movement.sqrMagnitude > 0.0001f;

            animator.SetBool("isMoving", isActuallyMoving);
        }
    }

    private float GetUnitSpeed()
    {
        if (unitInstance != null && unitInstance.unitData != null)
        {
            if (unitInstance.unitData.speed > 0f)
                return unitInstance.unitData.speed;
        }

        return moveSpeed;
    }

    public void MoveToPosition(Vector3 destination, float stopDistance)
    {
        Debug.Log($"MoveToPosition | stopDistance reçu: {stopDistance} | agent.stoppingDistance avant: {agent.stoppingDistance}");
        if (!CanMoveOnWorldPosition(destination))
            return;

        BoatTransport.ClearPendingBoarding(unitInstance);

        followTarget = null;
        targetPosition = destination;
        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;

        if (CanUseNavMeshAgent())
        {
            agent.speed = GetUnitSpeed();
            agent.areaMask = GetAllowedNavMeshAreaMask();
            agent.stoppingDistance = stoppingDistance;
            Debug.Log($"MoveToPosition | agent.stoppingDistance après: {agent.stoppingDistance}");
            agent.ResetPath();
            agent.SetDestination(destination);
        }
    }

    public void MoveToPositionAsGroup(Vector3 destination, float stopDistance)
    {
        UnitsAnimation animatedMover = GetComponent<UnitsAnimation>();
        if (animatedMover != null)
            animatedMover.StopAttackForMovement();

        MoveToPosition(destination, stopDistance);
    }

    public void MoveToTarget(Transform target, float stopDistance)
    {
        UnitsAnimation animatedMover = GetComponent<UnitsAnimation>();
        if (animatedMover != null)
            animatedMover.StopAttackForMovement();

        MoveToTargetInternal(target, Mathf.Max(0f, stopDistance));
    }

    private void MoveToTargetInternal(Transform target, float stopDistance)
    {
        if (target != null && !CanMoveOnWorldPosition(target.position))
            return;

        BoatTransport.ClearPendingBoarding(unitInstance);

        followTarget = target;

        if (target != null)
            targetPosition = target.position;

        stoppingDistance = Mathf.Max(0f, GetUnitAttackRange(stopDistance));
        isMovingToTarget = true;

        if (CanUseNavMeshAgent() && target != null)
        {
            agent.speed = GetUnitSpeed();
            agent.areaMask = GetAllowedNavMeshAreaMask();
            agent.stoppingDistance = stoppingDistance;
            agent.ResetPath();
            agent.SetDestination(target.position);
        }
    }

    public void StopMovement()
    {
        BoatTransport.ClearPendingBoarding(unitInstance);

        isMovingToTarget = false;
        movement = Vector3.zero;
        followTarget = null;

        if (animator != null)
            animator.SetBool("isMoving", false);

        if (CanUseNavMeshAgent())
            agent.ResetPath();
    }

    public bool IsMoving()
    {
        return isMovingToTarget;
    }

    private float GetUnitAttackRange(float fallback)
    {
        if (unitInstance != null && unitInstance.unitData is UnitCombatData combatData)
            return Mathf.Max(0f, combatData.attackRange);

        return fallback;
    }

    public bool CanMoveOnWorldPosition(Vector3 worldPosition)
    {
        MapGenerator map = GetMapGenerator();
        if (map == null)
            return true;

        if (!map.TryGetTileAtWorldPosition(worldPosition, out TileData tile))
            return false;

        bool allowed = IsBoatUnit() ? tile.groundType == GroundType.Water : tile.groundType != GroundType.Water;
        if (!allowed)
        {
            string unitType = unitInstance != null && unitInstance.unitData != null
                ? unitInstance.unitData.type.ToString()
                : "Unknown";
            Debug.Log($"[MovementManager] Destination refusée: unit={unitType}, tile={tile.groundType}, position={worldPosition}.", this);
        }

        return allowed;
    }

    private MapGenerator GetMapGenerator()
    {
        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();

        return mapGenerator;
    }

    private int GetAllowedNavMeshAreaMask()
    {
        if (IsBoatUnit())
        {
            int waterArea = NavMesh.GetAreaFromName("Water");
            return waterArea >= 0 ? 1 << waterArea : NavMesh.AllAreas;
        }

        int walkableArea = NavMesh.GetAreaFromName("Walkable");
        return walkableArea >= 0 ? 1 << walkableArea : NavMesh.AllAreas;
    }

    private bool IsBoatUnit()
    {
        if (unitInstance == null || unitInstance.unitData == null)
            return false;

        if (unitInstance.unitData is UnitBoat)
            return true;

        switch (unitInstance.unitData.type)
        {
            case UnitsType.Fregate:
            case UnitsType.Destroyer:
            case UnitsType.Transport:
                return true;
            default:
                return false;
        }
    }

    private bool CanUseNavMeshAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    public Vector3 GetMovementDirection()
    {
        return movement;
    }

    public System.Action<Transform> OnMovementComplete;

    private void NotifyMovementComplete()
    {
        if (OnMovementComplete != null)
        {
            OnMovementComplete(followTarget);
        }
    }
    
    public void MoveBoatsUnitToPositionAsGroup(UnitInstance unit, Vector3 destination, float stopDistance)
    {
        if (unit == null)
            return;

        MovementManager movement = unit.GetComponent<MovementManager>();
        if (movement == null || !movement.CanMoveOnWorldPosition(destination))
            return;

        movement.MoveToPositionAsGroup(destination, stopDistance);
    }

    public void MoveBoatsUnitToTarget(UnitInstance unit, Transform target, float stopDistance)
    {
        if (unit == null || target == null)
            return;

        MovementManager movement = unit.GetComponent<MovementManager>();
        if (movement == null || !movement.CanMoveOnWorldPosition(target.position))
            return;

        movement.MoveToTarget(target, stopDistance);
    }

    public void ForceMoveToPosition(Vector3 destination, float stopDistance)
    {
        followTarget = null;
        targetPosition = destination;
        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;

        if (CanUseNavMeshAgent())
        {
            agent.speed = GetUnitSpeed();
            agent.areaMask = GetAllowedNavMeshAreaMask();
            agent.stoppingDistance = stoppingDistance;
            agent.ResetPath();
            agent.SetDestination(destination);
        }
    }
}
