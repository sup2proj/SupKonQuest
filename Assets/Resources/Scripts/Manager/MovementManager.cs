using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using System.Collections.Generic;

public class MovementManager : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0.2f;
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

    private static int priorityCounter = 0;

    private void Awake()
    {
        unitInstance = GetComponent<UnitInstance>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        selectable = GetComponent<SelectableObject>();

        if (agent != null)
        {
            agent.speed = GetUnitSpeed();
            agent.avoidancePriority = Mathf.Clamp(priorityCounter % 99, 1, 99);
            priorityCounter++;
        }
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.speed = GetUnitSpeed();
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    private void Update()
    {
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
                targetPosition = hit.point;
                targetPosition.y = transform.position.y;
                isMovingToTarget = true;
            }
        }
    }

    private void HandleMovement()
    {
        if (agent != null)
            agent.speed = GetUnitSpeed();

        // Version optimisée utilisant NavMeshAgent
        if (agent != null)
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
                    agent.ResetPath();
                    stuckTimer = 0f;
                    NotifyMovementComplete();
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
            if (agent != null)
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
            return unitInstance.unitData.speed;
        }
        return moveSpeed;
    }

    public void MoveToPosition(Vector3 destination, float stopDistance)
    {
        followTarget = null;
        targetPosition = destination;
        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;

        if (agent != null)
        {
            agent.speed = GetUnitSpeed();
            agent.stoppingDistance = stoppingDistance;
            agent.ResetPath();
            agent.SetDestination(destination);
        }
    }

    public void MoveToTarget(Transform target, float stopDistance)
    {
        followTarget = target;

        if (target != null)
            targetPosition = target.position;

        stoppingDistance = Mathf.Max(0f, GetUnitAttackRange(stopDistance));
        isMovingToTarget = true;

        if (agent != null && target != null)
        {
            agent.speed = GetUnitSpeed();
            agent.stoppingDistance = stoppingDistance;
            agent.ResetPath();
            agent.SetDestination(target.position);
        }
    }

    public void StopMovement()
    {
        isMovingToTarget = false;
        movement = Vector3.zero;
        followTarget = null;

        if (animator != null)
            animator.SetBool("isMoving", false);

        if (agent != null)
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
}
