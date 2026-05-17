using UnityEngine;
using System.Collections;

public class UnitsAnimation : MonoBehaviour
{
    public Animator animator;

    private Transform attackTarget;
    private UnitInstance cachedUnit;
    private Coroutine attackCoroutine;
    private DamageTable damageTable;

    private MovementManager movementManager;

    public Transform AttackTarget => attackTarget;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        cachedUnit = GetComponent<UnitInstance>();
        movementManager = GetComponent<MovementManager>();

        damageTable = UnityEngine.Resources.Load<DamageTable>("Scripts/Data/Units/UnitsSO/DamageTable");
        if (damageTable == null)
            Debug.LogError("Impossible de charger DamageTable !");

        if (movementManager != null)
            movementManager.OnMovementComplete += OnMovementCompleted;
    }

    void Update()
    {
        HandleAutoAttack();
        RefreshMovingArcherModel();
    }

    public void AttackTheAttacker(Transform attacker = null)
    {
        if (attacker != null)
            attackTarget = attacker;

        if (attackTarget == null || !CanAttackWithDamage(cachedUnit))
            return;

        if (attackCoroutine != null)
        {
            SetAttackVisuals(true);
            return;
        }

        BeginAttackLoop();
    }

    public void StartAttackWithDamage()
    {
        UnitInstance unit = cachedUnit;
        if (!CanAttackWithDamage(unit))
        {
            Debug.Log("[UnitsAnimation] StartAttackWithDamage ignored for support/healer " + gameObject.name, this);
            return;
        }

        if (attackTarget == null)
            return;

        Debug.Log($"[UnitsAnimation] {gameObject.name} commence l'attaque contre {attackTarget.name}");
        BeginAttackLoop();
    }

    public bool TryStartAttackTargetIfInRange(Transform target)
    {
        if (target == null)
            return false;

        if (attackTarget == target && attackCoroutine != null)
            return true;

        if (!CanAttackWithDamage(cachedUnit))
            return false;

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

    private IEnumerator AttackLoopCoroutine()
    {
        while (true)
        {
            UnitInstance attackerUnit = cachedUnit;
            if (attackTarget == null || attackerUnit == null || attackerUnit.currentHealth <= 0)
                break;

            if (!IsTargetWithinAttackRange())
                break;

            AnimationClip attackClip = GetAttackClip();
            float attackDuration = attackClip != null ? attackClip.length : 1f;
            float halfDuration = Mathf.Max(0.05f, attackDuration * 0.5f);
            SetAttackAnimationState(true);
            yield return new WaitForSeconds(halfDuration);

            if (!IsTargetWithinAttackRange())
                break;
            if (!TryGetCurrentTarget(out UnitInstance targetUnit, out StructureInstance targetStructure))
                break;
            if (IsFriendlyStructureTarget(targetStructure, attackerUnit))
                break;

            float attack = GetAttackDamage(targetUnit, targetStructure);
            ApplyAttackDamage(targetUnit, targetStructure, attackerUnit, attack);

            yield return new WaitForSeconds(halfDuration);
        }

        StopAttackInternal();
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

    private void SetAttackAnimationState(bool isAttacking)
    {
        if (animator != null)
            animator.SetBool("isAttacking", isAttacking);
    }

    private void StopAttackInternal()
    {
        StopAttackCoroutine();
        SetAttackVisuals(false);
        attackTarget = null;
    }

    public void StopAttackForMovement()
    {
        attackTarget = null;
        StopAttackInternal();
    }

    void HandleAutoAttack()
    {
        UnitInstance unit = cachedUnit;
        if (!CanAttackWithDamage(unit))
            return;

        if (attackTarget == null)
        {
            SetAttackVisuals(false);
            return;
        }

        if (!TryGetCurrentTarget(out UnitInstance targetUnit, out StructureInstance targetStructure))
        {
            StopAttackInternal();
            return;
        }

        if (IsFriendlyStructureTarget(targetStructure, unit))
        {
            StopAttackInternal();
            return;
        }
        if (!IsTargetWithinAttackRange())
        {
            StopAttackInternal();
            return;
        }
        if (attackCoroutine != null)
            SetAttackVisuals(true);
    }

    private void RefreshMovingArcherModel()
    {
        UnitInstance unit = cachedUnit;
        if (animator == null || movementManager == null || unit == null || unit.objectModel == null || unit.unitData == null)
            return;

        if (unit.unitData.type == UnitsType.Archer && movementManager.IsMoving())
            unit.objectModel.SetActive(true);
    }

    private bool CanAttackWithDamage(UnitInstance unit)
    {
        if (unit == null || unit.unitData == null)
            return false;

        return unit.unitData.type != UnitsType.Support && unit.unitData.type != UnitsType.Healer;
    }

    private void SetAttackVisuals(bool visible)
    {
        SetAttackAnimationState(visible);
        SetObjectModelVisible(visible);
    }

    private void SetObjectModelVisible(bool visible)
    {
        UnitInstance unit = cachedUnit;
        if (unit == null)
            unit = GetComponent<UnitInstance>();

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(visible);
    }

    private void BeginAttackLoop()
    {
        SetAttackVisuals(true);
        StopAttackCoroutine();
        attackCoroutine = StartCoroutine(AttackLoopCoroutine());
    }

    private void StopAttackCoroutine()
    {
        if (attackCoroutine == null)
            return;

        StopCoroutine(attackCoroutine);
        attackCoroutine = null;
    }

    private bool TryGetCurrentTarget(out UnitInstance targetUnit, out StructureInstance targetStructure)
    {
        targetUnit = GetTargetUnit();
        targetStructure = targetUnit == null ? GetTargetStructure() : null;

        if (targetUnit == null && targetStructure == null)
            return false;

        if (targetUnit != null && targetUnit.currentHealth <= 0)
            return false;

        if (targetStructure != null && targetStructure.currentHealth <= 0)
            return false;

        return true;
    }

    private bool IsFriendlyStructureTarget(StructureInstance targetStructure, UnitInstance attackerUnit)
    {
        return targetStructure != null && attackerUnit != null && targetStructure.playerId == attackerUnit.playerId;
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

    private UnitInstance GetTargetUnit()
    {
        if (attackTarget == null)
            return null;

        UnitInstance unit = attackTarget.GetComponent<UnitInstance>();
        if (unit == null)
            unit = attackTarget.GetComponentInParent<UnitInstance>();
        if (unit == null)
            unit = attackTarget.GetComponentInChildren<UnitInstance>();

        return unit;
    }

    private StructureInstance GetTargetStructure()
    {
        if (attackTarget == null)
            return null;

        StructureInstance structure = attackTarget.GetComponent<StructureInstance>();
        if (structure == null)
            structure = attackTarget.GetComponentInParent<StructureInstance>();
        if (structure == null)
            structure = attackTarget.GetComponentInChildren<StructureInstance>();

        return structure;
    }

    private float GetAttackDamage(UnitInstance targetUnit, StructureInstance targetStructure)
    {
        UnitInstance attacker = cachedUnit;
        if (attacker == null)
        {
            Debug.LogError("attacker NULL");
            return 0f;
        }

        if (!(attacker.unitData is UnitCombatData combatData))
        {
            Debug.LogError("attacker.unitData NULL");
            return 0f;
        }

        if (targetStructure != null)
        {
            if (targetStructure.playerId == attacker.playerId)
                return 0f;

            Debug.Log($"[Attaque] {attacker.name} frappe structure pour {combatData.attack} degats");
            return Mathf.Max(0f, combatData.attack);
        }

        if (targetUnit == null)
        {
            Debug.LogError("target NULL");
            return 0f;
        }

        if (targetUnit.unitData == null)
        {
            Debug.LogError("target.unitData NULL");
            return 0f;
        }

        if (damageTable == null)
        {
            Debug.LogError("damageTable NULL");
            return 0f;
        }

        float damage = combatData.attack * damageTable.GetMultiplier(attacker, targetUnit);
        Debug.Log($"[Attaque] {attacker.name} frappe {targetUnit.name} pour {damage} degats");
        return Mathf.Max(0f, damage);
    }

    private void ApplyAttackDamage(UnitInstance targetUnit, StructureInstance targetStructure, UnitInstance attackerUnit, float attack)
    {
        if (targetUnit != null)
        {
            targetUnit.TakeDamage(attack);

            UnitsAnimation targetAnimation = targetUnit.GetComponent<UnitsAnimation>();
            if (targetAnimation != null && attackerUnit != null)
                targetAnimation.AttackTheAttacker(transform);

            return;
        }

        if (targetStructure != null)
            targetStructure.TakeDamage(attack, attackerUnit);
    }

    private void OnMovementCompleted(Transform target)
    {
        if (target == null)
            return;

        attackTarget = target;
        if (CanAttackWithDamage(cachedUnit))
            StartAttackWithDamage();
    }


    public void EngageTarget(Transform target, float stopDistance)
    {
        if (target == null || movementManager == null)
            return;

        float desiredStopDistance = Mathf.Max(0f, stopDistance);
        bool alreadyMovingToTarget = movementManager.IsMoving();
        bool alreadyAttackingTarget = attackTarget == target;

        if (alreadyMovingToTarget || alreadyAttackingTarget)
            return;

        movementManager.MoveToTarget(target, desiredStopDistance);
    }

    public void StartAttackAnimationFromSpell()
    {
        SetAttackVisuals(true);
        StartCoroutine(ResetSpellAttackToIdleAfterAnimation());
    }

    private IEnumerator ResetSpellAttackToIdleAfterAnimation()
    {
        AnimationClip attackClip = GetAttackClip();
        float duration = attackClip != null ? attackClip.length : 0.05f;
        duration = Mathf.Max(0.05f, duration);

        yield return new WaitForSeconds(duration);

        SetAttackVisuals(false);
    }
}
