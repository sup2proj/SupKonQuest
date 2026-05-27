using UnityEngine;

public partial class UnitsAnimation : MonoBehaviour
{
    private void OnMovementCompleted(Transform target)
    {
        if (isGroupLeader && activeGroupMoveId >= 0)
            completedGroupMoves.Add(activeGroupMoveId);

        if (target != null)
        {
            attackTarget = target;

            UnitInstance unit = cachedUnit;
            if (unit != null && unit.unitData != null)
            {
                if (unit.unitData.type != UnitsType.Healer &&
                    unit.unitData.type != UnitsType.Support)
                {
                    StartAttackWithDamage();
                }
            }
        }
    }

    private void ClearGroupMoveState()
    {
        activeGroupMoveId = -1;
        isGroupLeader = false;
    }

    public void MoveToPosition(Vector3 destination, float stopDistance)
    {
        attackTarget = null;
        StopAttackInternal();
        ClearGroupMoveState();

        if (movementManager != null)
        {
            movementManager.MoveToPosition(destination, stopDistance);
        }
    }

    public void MoveToTarget(Transform target, float stopDistance)
    {
        attackTarget = null;
        StopAttackInternal();
        ClearGroupMoveState();

        if (movementManager != null)
        {
            movementManager.MoveToTarget(target, GetCurrentAttackRange());
        }
    }

    public void EngageTarget(Transform target, float stopDistance)
    {
        if (target == null || movementManager == null)
            return;

        float desiredStopDistance = Mathf.Max(0f, stopDistance);
        bool alreadyMovingToTarget = movementManager.IsMoving();
        bool alreadyAttackingTarget = attackTarget == target;

        if ((alreadyMovingToTarget || alreadyAttackingTarget))
            return;

        MoveToTarget(target, desiredStopDistance);
    }

    public void MoveToPositionAsGroup(Vector3 destination, float stopDistance, int groupMoveId, bool isLeader)
    {
        activeGroupMoveId = groupMoveId;
        isGroupLeader = isLeader;
        attackTarget = null;
        StopAttackInternal();

        if (movementManager != null)
        {
            movementManager.MoveToPosition(destination, stopDistance);
        }
    }
}