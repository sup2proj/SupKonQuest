using Enums.Environment;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MovementManager : MonoBehaviour
{
    public static MovementManager Instance;

    private const float NavMeshSampleDistance = 2.0f;
    private const float AgentStoppedSqrVelocity = 0.01f;
    private const float AgentStuckVelocityThreshold = 0.05f;
    private const float AgentStuckTimeout = 1.5f;
    private const float AnimationMoveVelocityThreshold = 0.1f;
    private const float TransformMoveSqrThreshold = 0.0001f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0.3f;
    public float rotationSpeed = 12f;

    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private Transform followTarget;
    private bool followTargetPosition = true;
    private float stuckTimer = 0f;

    private UnitInstance unitInstance;
    private NavMeshAgent agent;
    private Animator animator;
    private MapGenerator mapGenerator;

    private static int priorityCounter = 0;

    private void Awake()
    {
        Instance = this;
        unitInstance = GetComponent<UnitInstance>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent != null)
        {
            RefreshAgentSettings();
            agent.avoidancePriority = Mathf.Clamp(priorityCounter % 99, 1, 99);
            priorityCounter++;
        }
    }

    private void Start()
    {
        if (agent == null)
            return;

        RefreshAgentSettings();
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSampleDistance, agent.areaMask))
            agent.Warp(hit.position);
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

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Vector3 destination = hit.point;
        destination.y = transform.position.y;
        MoveToPosition(destination, stoppingDistance);
    }

    private void HandleMovement()
    {
        RefreshAgentSettings();

        if (CanUseNavMeshAgent())
            HandleNavMeshMovement();
        else
            HandleTransformMovement();

        UpdateMovementAnimation();
    }

    private void HandleNavMeshMovement()
    {
        if (!isMovingToTarget)
            return;

        if (followTarget != null && followTargetPosition)
            agent.SetDestination(followTarget.position);

        if (HasReachedAgentDestination())
        {
            StopNavMeshMovement(notifyComplete: true, clearVelocity: false);
            return;
        }

        if (IsAgentStuckTimedOut())
            StopNavMeshMovement(notifyComplete: false, clearVelocity: true);
    }

    private bool HasReachedAgentDestination()
    {
        float dynamicStoppingDistance = Mathf.Max(agent.stoppingDistance, agent.speed * 0.1f);
        return !agent.pathPending
            && agent.remainingDistance <= dynamicStoppingDistance
            && agent.velocity.sqrMagnitude < AgentStoppedSqrVelocity;
    }

    private bool IsAgentStuckTimedOut()
    {
        if (agent.velocity.magnitude >= AgentStuckVelocityThreshold)
        {
            stuckTimer = 0f;
            return false;
        }

        stuckTimer += Time.deltaTime;
        return stuckTimer > AgentStuckTimeout;
    }

    private void StopNavMeshMovement(bool notifyComplete, bool clearVelocity)
    {
        isMovingToTarget = false;
        stuckTimer = 0f;

        if (clearVelocity)
            agent.velocity = Vector3.zero;

        agent.ResetPath();

        if (notifyComplete)
            NotifyMovementComplete();
    }

    private void HandleTransformMovement()
    {
        if (!isMovingToTarget)
        {
            movement = Vector3.zero;
            return;
        }

        if (followTarget != null && followTargetPosition)
            targetPosition = followTarget.position;

        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stoppingDistance)
        {
            isMovingToTarget = false;
            movement = Vector3.zero;
            NotifyMovementComplete();
            return;
        }

        movement = toTarget.normalized;

        transform.position += movement * GetUnitSpeed() * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool("isMoving", IsActuallyMoving());
    }

    private bool IsActuallyMoving()
    {
        if (CanUseNavMeshAgent())
            return isMovingToTarget && agent.velocity.magnitude > AnimationMoveVelocityThreshold;

        return isMovingToTarget && movement.sqrMagnitude > TransformMoveSqrThreshold;
    }

    private float GetUnitSpeed()
    {
        if (unitInstance != null && unitInstance.unitData != null && unitInstance.unitData.speed > 0f)
            return unitInstance.unitData.speed;

        return moveSpeed;
    }

    private void RefreshAgentSettings()
    {
        if (agent == null)
            return;

        agent.speed = GetUnitSpeed();
        agent.areaMask = GetAllowedNavMeshAreaMask();
    }

    public void MoveToPosition(Vector3 destination, float stopDistance)
    {
        if (!CanMoveOnWorldPosition(destination))
            return;

        SetPositionDestination(destination, stopDistance, clearPendingBoarding: true);
    }

    public void MoveToPositionAsGroup(Vector3 destination, float stopDistance)
    {
        StopAttackForMovement();
        MoveToPosition(destination, stopDistance);
    }

    public void MoveToTarget(Transform target, float stopDistance)
    {
        StopAttackForMovement();
        MoveToTargetInternal(target, Mathf.Max(0f, stopDistance));
    }

    private void MoveToTargetInternal(Transform target, float stopDistance)
    {
        BoatTransport.ClearPendingBoarding(unitInstance);

        followTarget = target;
        followTargetPosition = false;

        Vector3 destination = target != null ? target.position : transform.position;
        if (target != null)
        {
            StructureInstance structure = target.GetComponent<StructureInstance>();
            if (structure == null)
                structure = target.GetComponentInParent<StructureInstance>();
            if (structure == null)
                structure = target.GetComponentInChildren<StructureInstance>();

            if (structure != null)
            {
                followTargetPosition = false;
                if (!TryGetApproachDestinationForTarget(target, stopDistance, out destination))
                    return;
            }
            else if (CanMoveOnWorldPosition(target.position))
            {
                followTargetPosition = true;
                destination = target.position;
            }
            else if (!TryGetApproachDestinationForTarget(target, stopDistance, out destination))
            {
                return;
            }
        }

        if (target != null)
            targetPosition = destination;

        StartMovement(GetUnitAttackRange(stopDistance));

        if (target != null)
            ApplyAgentDestination(destination);
    }
    
    // On va chercher le point le plus proche pour que les unités n'aillent pas dans l'eau.
    private bool TryGetApproachDestinationForTarget(Transform target, float stopDistance, out Vector3 destination)
    {
        destination = target != null ? target.position : transform.position;

        if (target == null)
            return false;

        StructureInstance structure = target.GetComponent<StructureInstance>();
        if (structure == null)
            structure = target.GetComponentInParent<StructureInstance>();
        if (structure == null)
            structure = target.GetComponentInChildren<StructureInstance>();

        if (structure == null)
            return false;

        int areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas;
        Vector3 center = structure.StructurePosition;
        float baseRadius = Mathf.Max(0.75f, stopDistance);
        float maxRadius = baseRadius + 8f;
        const int directionCount = 8;

        for (float radius = baseRadius; radius <= maxRadius; radius += 1f)
        {
            for (int i = 0; i < directionCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / directionCount;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, areaMask))
                {
                    destination = hit.position;
                    return true;
                }
            }
        }

        if (NavMesh.SamplePosition(center, out NavMeshHit centerHit, NavMeshSampleDistance * 2f, areaMask))
        {
            destination = centerHit.position;
            return true;
        }

        return false;
    }

    private void SetPositionDestination(Vector3 destination, float stopDistance, bool clearPendingBoarding)
    {
        if (clearPendingBoarding)
            BoatTransport.ClearPendingBoarding(unitInstance);

        followTarget = null;
        followTargetPosition = true;
        targetPosition = destination;
        StartMovement(stopDistance);
        ApplyAgentDestination(destination);
    }

    private void StartMovement(float stopDistance)
    {
        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;
    }

    private void ApplyAgentDestination(Vector3 destination)
    {
        if (!CanUseNavMeshAgent())
            return;

        RefreshAgentSettings();
        agent.stoppingDistance = stoppingDistance;
        agent.ResetPath();
        agent.SetDestination(destination);
    }

    private void StopAttackForMovement()
    {
        UnitsAnimation animatedMover = GetComponent<UnitsAnimation>();
        if (animatedMover != null)
            animatedMover.StopAttackForMovement();
    }

    public void StopMovement()
    {
        BoatTransport.ClearPendingBoarding(unitInstance);

        isMovingToTarget = false;
        movement = Vector3.zero;
        followTarget = null;
        followTargetPosition = true;

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

        bool allowed = IsGroundAllowed(tile.groundType);
        if (!allowed)
            Debug.Log($"[MovementManager] Destination refusee: unit={GetUnitTypeName()}, tile={tile.groundType}, position={worldPosition}.", this);

        return allowed;
    }

    private bool IsGroundAllowed(GroundType groundType)
    {
        return IsBoatUnit() ? groundType == GroundType.Water : groundType != GroundType.Water;
    }

    private string GetUnitTypeName()
    {
        return unitInstance != null && unitInstance.unitData != null
            ? unitInstance.unitData.type.ToString()
            : "Unknown";
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
            OnMovementComplete(followTarget);
    }

    public void MoveBoatsUnitToPositionAsGroup(UnitInstance unit, Vector3 destination, float stopDistance)
    {
        if (!TryGetMovementForUnit(unit, destination, out MovementManager movement))
            return;

        movement.MoveToPositionAsGroup(destination, stopDistance);
    }

    public void MoveBoatsUnitToTarget(UnitInstance unit, Transform target, float stopDistance)
    {
        if (unit == null || target == null)
            return;

        MovementManager movement = unit.GetComponent<MovementManager>();
        if (movement == null)
            return;

        movement.MoveToTarget(target, stopDistance);
    }

    private bool TryGetMovementForUnit(UnitInstance unit, Vector3 destination, out MovementManager movement)
    {
        movement = null;
        if (unit == null)
            return false;

        movement = unit.GetComponent<MovementManager>();
        return movement != null && movement.CanMoveOnWorldPosition(destination);
    }

    public void ForceMoveToPosition(Vector3 destination, float stopDistance)
    {
        SetPositionDestination(destination, stopDistance, clearPendingBoarding: false);
    }
}
