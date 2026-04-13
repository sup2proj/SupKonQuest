using UnityEngine;
using System.Collections.Generic;

public class UnitsAnimation : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 1f;

    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private float stoppingDistance = 0.1f;
    private Transform followTarget;

    private Transform attackTarget;
    private int activeGroupMoveId = -1;
    private bool isGroupLeader = false;

    private static readonly HashSet<int> completedGroupMoves = new HashSet<int>();

    private UnitInstance cachedUnit;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        cachedUnit = GetComponent<UnitInstance>();
    }

    void Update()
    {
        HandleMovement();
        HandleAutoAttack();

        if (animator != null)
        {
            animator.SetBool("isMoving", isMovingToTarget);
            UnitInstance unit = cachedUnit;
            if (unit != null && unit.objectModel != null && unit.unitData != null)
            {
                if (unit.unitData.type == UnitsType.Archer && isMovingToTarget)
                {
                    unit.objectModel.SetActive(true);
                }
            }
        }
    }

    void HandleMovement()
    {
        if (!isMovingToTarget)
        {
            movement = Vector3.zero;
            return;
        }

        // Si le meneur de ce groupe est déjà arrivé, tous les autres s'arrêtent.
        if (!isGroupLeader && activeGroupMoveId >= 0 && completedGroupMoves.Contains(activeGroupMoveId))
        {
            StopMovementInternal();
            return;
        }

        if (followTarget != null)
            targetPosition = followTarget.position;

        // Distance en XZ pour éviter les soucis de hauteur
        Vector3 flatCurrent = transform.position;
        Vector3 flatTarget = targetPosition;
        flatCurrent.y = 0f;
        flatTarget.y = 0f;

        float distance = Vector3.Distance(flatCurrent, flatTarget);

        if (distance <= stoppingDistance)
        {
            StopMovementInternal();

            if (isGroupLeader && activeGroupMoveId >= 0)
                completedGroupMoves.Add(activeGroupMoveId);

            // Si on suivait une unite ennemie: démarre la boucle d'attaque
            if (followTarget != null)
            {
                attackTarget = followTarget;

                UnitInstance unit = cachedUnit;
                if (unit != null && unit.unitData != null)
                {
                    // Seules les unités de combat déclenchent l'attaque automatique.
                    if (unit.unitData.type != UnitsType.Healer &&
                        unit.unitData.type != UnitsType.Support)
                    {
                        if (animator != null)
                            animator.SetBool("isAttacking", true);

                        if (unit.objectModel != null)
                            unit.objectModel.SetActive(true);
                    }
                }
            }

            followTarget = null;
            return;
        }

        Vector3 direction = (flatTarget - flatCurrent).normalized;
        movement = new Vector3(direction.x, 0f, direction.z);

        transform.position += movement * moveSpeed * Time.deltaTime;

        if (movement.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    void HandleAutoAttack()
    {
        UnitInstance unit = cachedUnit;
        if (attackTarget == null)
        {
            if (animator != null)
                animator.SetBool("isAttacking", false);

            if (unit != null && unit.objectModel != null)
                unit.objectModel.SetActive(false);

            return;
        }

        UnitInstance targetUnit = attackTarget.GetComponent<UnitInstance>();

        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = attackTarget.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);
    }

    private void StopMovementInternal()
    {
        isMovingToTarget = false;
        movement = Vector3.zero;

        if (animator != null)
            animator.SetBool("isMoving", false);
    }

    private void ClearGroupMoveState()
    {
        activeGroupMoveId = -1;
        isGroupLeader = false;
    }

    public void MoveToPosition(Vector3 destination, float stopDistance)
    {
        followTarget = null;
        attackTarget = null;
        ClearGroupMoveState();

        targetPosition = destination;
        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;
    }

    public void MoveToTarget(Transform target, float stopDistance)
    {
        followTarget = target;
        attackTarget = null;
        ClearGroupMoveState();

        if (target != null)
            targetPosition = target.position;

        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;
    }

    public void MoveToPositionAsGroup(Vector3 destination, float stopDistance, int groupMoveId, bool isLeader)
    {
        activeGroupMoveId = groupMoveId;
        isGroupLeader = isLeader;
        followTarget = null;
        attackTarget = null;

        targetPosition = destination;
        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;
    }
}
