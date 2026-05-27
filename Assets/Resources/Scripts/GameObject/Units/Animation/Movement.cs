using UnityEngine;

public partial class UnitsAnimation : MonoBehaviour
{
    /// <summary>
    /// Termine un déplacement et déclenche éventuellement une attaque sur la cible atteinte.
    /// </summary>
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

    /// <summary>
    /// Réinitialise l'état interne lié au déplacement de groupe.
    /// </summary>
    private void ClearGroupMoveState()
    {
        activeGroupMoveId = -1;
        isGroupLeader = false;
    }

    /// <summary>
    /// Lance un déplacement vers une position précise.
    /// </summary>
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

    /// <summary>
    /// Lance un déplacement vers une cible en utilisant la portée d'attaque actuelle.
    /// </summary>
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

    /// <summary>
    /// Engage automatiquement la cible si l'unité ne la poursuit pas déjà.
    /// </summary>
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

    /// <summary>
    /// Lance un déplacement en groupe avec l'identifiant et le rôle de leader fournis.
    /// </summary>
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