using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class UnitsAnimation : MonoBehaviour
{
    public void AttackTheAttacker(Transform attacker = null)
    {
        UnitInstance selfUnit = cachedUnit;
        if (attacker != null)
        {
            attackTarget = attacker;
        }
        if (attackTarget == null)
            return;

        if (attackCoroutine != null)
        {
            UnitInstance currentTargetUnit = attackTarget.GetComponent<UnitInstance>();
            if (currentTargetUnit != null)
            {
                if (animator != null)
                    animator.SetBool("isAttacking", true);
                if (selfUnit != null && selfUnit.objectModel != null)
                    selfUnit.objectModel.SetActive(true);
                return;
            }
        }
        if (animator != null && selfUnit != null && selfUnit.objectModel != null)
        {
            animator.SetBool("isAttacking", true);
            selfUnit.objectModel.SetActive(true);
        }
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        attackCoroutine = StartCoroutine(AttackLoopCoroutine());
    }

    public void StartAttackWithDamage()
    {
        UnitInstance unit = cachedUnit;
        if (unit != null && unit.unitData != null && (unit.unitData.type == UnitsType.Support || unit.unitData.type == UnitsType.Healer))
        {
            Debug.Log("[UnitsAnimation] StartAttackWithDamage ignored for support/healer " + gameObject.name, this);
            return;
        }

        isRetaliating = false;
        SetAttackAnimationState(true);

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(true);

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        attackCoroutine = StartCoroutine(AttackLoopCoroutine());
    }

    private IEnumerator AttackLoopCoroutine()
    {
        while (true)
        {
            if (attackTarget == null)
                break;

            UnitInstance attackerUnit = cachedUnit;
            if (attackerUnit == null || attackerUnit.currentHealth <= 0)
                break;

            if (!isRetaliating && !IsTargetWithinAttackRange())
                break;

            AnimationClip attackClip = GetAttackClip();
            float attackDuration = attackClip != null ? attackClip.length : 1f;
            float halfDuration = Mathf.Max(0.05f, attackDuration * 0.5f);
            SetAttackAnimationState(true);
            yield return new WaitForSeconds(halfDuration);

            if (!isRetaliating && !IsTargetWithinAttackRange())
                break;

            UnitInstance targetUnit = attackTarget.GetComponent<UnitInstance>();
            StructureInstance targetStructure = null;

            if (targetUnit == null)
                targetStructure = attackTarget.GetComponent<StructureInstance>();

            if (targetUnit == null && targetStructure == null)
                break;

            if (targetUnit != null && targetUnit.currentHealth <= 0)
                break;
            if (targetStructure != null && targetStructure.currentHealth <= 0)
                break;

            float attack = GetAttackDamage();

            if (cachedUnit != null && cachedUnit.unitData != null && (cachedUnit.unitData.type == UnitsType.Mortar || cachedUnit.unitData.type == UnitsType.Fregate))
            {
                if (cannonBallPrefab != null && attackTarget != null)
                {
                    MortarDamage(targetUnit, targetStructure, attackerUnit, attack);
                }
            }
            else
            {
                if (targetUnit != null)
                {
                    targetUnit.TakeDamage(attack);
                    UnitsAnimation targetAnimation = targetUnit.GetComponent<UnitsAnimation>();
                    if (targetAnimation != null && attackerUnit != null)
                        targetAnimation.AttackTheAttacker(transform);
                }
                else
                {
                    targetStructure.TakeDamage(attack, attackerUnit);
                }
            }

            yield return new WaitForSeconds(halfDuration);
        }

        StopAttackInternal();
    }
    
    private void MortarDamage(UnitInstance targetUnit, StructureInstance targetStructure, UnitInstance attackerUnit, float attack)
    {
        Transform targetSnapshot = attackTarget;
        UnitInstance targetUnitSnapshot = targetUnit;
        StructureInstance targetStructureSnapshot = targetStructure;
        UnitInstance attackerSnapshot = attackerUnit;
        Transform launcherTransform = transform;

        Vector3 spawnPos = transform.position - transform.forward * 0.5f + Vector3.up * 0.5f;
        float impactRadius = 1.5f;

        CannonBall.Spawn(cannonBallPrefab, spawnPos, targetSnapshot, cannonBallSpeed, (impactPos) => {

            GameObject impactZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            impactZone.transform.position = impactPos;
            impactZone.transform.localScale = new Vector3(impactRadius * 2f, 0.05f, impactRadius * 2f);
            impactZone.GetComponent<Collider>().enabled = false;
            impactZone.GetComponent<Renderer>().material.color = new Color(1f, 0f, 0f, 0.5f);
            Destroy(impactZone, 1f);
            UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(FindObjectsSortMode.None);
            foreach (UnitInstance hitUnit in allUnits)
            {
                if (hitUnit == null || hitUnit == attackerSnapshot)
                    continue;

                if (hitUnit.playerId == attackerSnapshot.playerId)
                    continue;

                if (hitUnit.currentHealth <= 0)
                    continue;

                float dist = Vector3.Distance(hitUnit.transform.position, impactPos);
                if (dist <= impactRadius)
                {
                    Debug.Log($"[Mortar] Dégâts sur {hitUnit.name} (dist={dist:F2})");
                    float damageMultiplier = 1f + (impactRadius - dist);
                    hitUnit.TakeDamage(attack * damageMultiplier);
                    UnitsAnimation anim = hitUnit.GetComponent<UnitsAnimation>();
                    if (anim != null)
                        anim.AttackTheAttacker(transform);
                }
            }

            // Structures
            StructureInstance[] allStructures = FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
            foreach (StructureInstance hitStructure in allStructures)
            {
                if (hitStructure == null || hitStructure.playerId == attackerSnapshot.playerId)
                    continue;

                float dist = Vector3.Distance(hitStructure.transform.position, impactPos);
                if (dist <= impactRadius)
                    hitStructure.TakeDamage(attack, attackerSnapshot);
            }

        }, transform);   
    }

    private AnimationClip GetAttackClip()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return null;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name.Contains("Attack"))
                return clip;
        }

        return null;
    }

    private void EnsureAttackAnimationStateAfterCoroutineStart()
    {
        if (ensureAttackAnimationCoroutine != null)
            StopCoroutine(ensureAttackAnimationCoroutine);

        ensureAttackAnimationCoroutine = StartCoroutine(EnsureAttackAnimationStateNextFrame());
    }

    private IEnumerator EnsureAttackAnimationStateNextFrame()
    {
        yield return null;

        if (attackCoroutine != null && attackTarget != null)
        {
            SetAttackAnimationState(true);

            UnitInstance unit = cachedUnit;
            if (unit != null && unit.objectModel != null)
                unit.objectModel.SetActive(true);
        }

        ensureAttackAnimationCoroutine = null;
    }

    private void SetAttackAnimationState(bool isAttacking)
    {
        if (animator != null)
            animator.SetBool("isAttacking", isAttacking);
    }

    private void StopAttackInternal()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        SetAttackAnimationState(false);
        isRetaliating = false;

        if (ensureAttackAnimationCoroutine != null)
        {
            StopCoroutine(ensureAttackAnimationCoroutine);
            ensureAttackAnimationCoroutine = null;
        }

        UnitInstance unit = cachedUnit;
        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(false);
        attackTarget = null;
    }

    void HandleAutoAttack()
    {
        UnitInstance unit = cachedUnit;
        if (unit != null && unit.unitData != null && (unit.unitData.type == UnitsType.Support || unit.unitData.type == UnitsType.Healer))
        {
            return;
        }

        if (attackTarget == null)
        {
            SetAttackAnimationState(false);
            if (unit != null && unit.objectModel != null)
                unit.objectModel.SetActive(false);
            return;
        }

        UnitInstance targetUnit = attackTarget.GetComponent<UnitInstance>();
        StructureInstance targetStructure = null;
        if (targetUnit == null)
            targetStructure = attackTarget.GetComponent<StructureInstance>();

        // Vérifier si la cible existe et est encore en vie
        if (targetUnit == null && targetStructure == null)
        {
            StopAttackInternal();
            return;
        }

        // Vérifier si la cible est encore en vie
        if (targetUnit != null && targetUnit.currentHealth <= 0)
        {
            StopAttackInternal();
            return;
        }
        if (targetStructure != null && targetStructure.currentHealth <= 0)
        {
            StopAttackInternal();
            return;
        }

        if (!isRetaliating && !IsTargetWithinAttackRange())
        {
            StopAttackInternal();
            return;
        }

        if (attackCoroutine != null)
        {
            SetAttackAnimationState(true);

            if (unit != null && unit.objectModel != null)
                unit.objectModel.SetActive(true);
        }
    }

    private float GetCurrentAttackRange()
    {
        UnitInstance unit = cachedUnit;
        if (unit != null && unit.unitData is UnitCombatData combatData)
            return Mathf.Max(0f, combatData.attackRange);
        return 0.1f;
    }

    private bool IsTargetWithinAttackRange()
    {
        if (attackTarget == null)
            return false;

        float attackRange = GetCurrentAttackRange();
        float allowedRange = attackRange + 0.05f;
        return GetFlatDistanceSqToAttackTarget() <= allowedRange * allowedRange;
    }

    private float GetFlatDistanceSqToAttackTarget()
    {
        Vector3 selfPosition = transform.position;
        float bestDistanceSq = float.MaxValue;

        Collider[] targetColliders = attackTarget.GetComponentsInChildren<Collider>();
        for (int i = 0; i < targetColliders.Length; i++)
        {
            Collider targetCollider = targetColliders[i];
            if (targetCollider == null || targetCollider.isTrigger)
                continue;

            Vector3 closestPoint = targetCollider.ClosestPoint(selfPosition);
            Vector3 toPoint = closestPoint - selfPosition;
            toPoint.y = 0f;

            float distanceSq = toPoint.sqrMagnitude;
            if (distanceSq < bestDistanceSq)
                bestDistanceSq = distanceSq;
        }

        if (bestDistanceSq < float.MaxValue)
            return bestDistanceSq;

        Vector3 toTarget = attackTarget.position - selfPosition;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude;
    }

    private float GetAttackDamage()
    {
        UnitInstance attacker = cachedUnit;
        UnitInstance target = attackTarget != null ? attackTarget.GetComponent<UnitInstance>() : null;
        StructureInstance targetStructure = attackTarget != null ? attackTarget.GetComponent<StructureInstance>() : null;

        if (attacker != null && attacker.unitData is UnitCombatData combatData)
        {
            if (attacker == null)
            {
                Debug.LogError("attacker NULL");
                return 0;
            }
            return Mathf.Max(0f, combatData.attack * damageTable.GetMultiplier(attacker, target != null ? target : null, targetStructure != null ? targetStructure : null));
        }

        return 0f;
    }

    public void StopAttackForMovement()
    {
        attackTarget = null;
        StopAttackInternal();
    }

    public bool TryStartAttackTargetIfInRange(Transform target)
    {
        if (target == null)
            return false;

        if (attackTarget == target && attackCoroutine != null)
            return true;

        Transform previousTarget = attackTarget;
        attackTarget = target;
        if (!IsTargetWithinAttackRange())
        {
            attackTarget = previousTarget;
            return false;
        }

        if (movementManager != null)
            movementManager.StopMovement();

        StartAttackWithDamage();
        return true;
    }
}