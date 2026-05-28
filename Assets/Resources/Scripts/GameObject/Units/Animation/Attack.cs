using UnityEngine;
using System.Collections;

/// <summary>
/// Méthodes liées aux attaques (corps-à-corps, projectiles, mortier) de l'unité.
/// Ce fichier contient la partie attaque de la classe partielle `UnitsAnimation`.
/// </summary>
public partial class UnitsAnimation : MonoBehaviour
{
	/// <summary>
	/// Lance une attaque de riposte contre l'attaquant fourni ou la cible courante.
	/// </summary>
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

	/// <summary>
	/// Démarre une attaque classique avec dégâts, en ignorant les unités de soutien et de soin.
	/// </summary>
	public void StartAttackWithDamage()
	{
		UnitInstance unit = cachedUnit;
		if (!CanAttackWithDamage(unit))
		{
			Debug.Log("[UnitsAnimation] StartAttackWithDamage ignored for support/healer " + gameObject.name, this);
			return;
		}

		if (attackTarget == null)
		{
			Debug.LogWarning("[StartAttackWithDamage] attackTarget null");
			return;
		}

		BeginAttackLoop();
	}

	/// <summary>
	/// Tente de démarrer une attaque si la cible donnée est à portée.
	/// </summary>
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

	/// <summary>
	/// Boucle d'attaque principale pour les unités non-projetiles.
	/// </summary>
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

            float attackSpeed = GetAttackSpeed();

            if (animator != null)
                	animator.speed = attackSpeed;

            float attackDuration = attackClip != null
                ? attackClip.length / attackSpeed
                : 1f;

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

            float attack = GetAttackDamage(targetUnit, targetStructure);

            if (cachedUnit != null && cachedUnit.unitData != null && cachedUnit.unitData.type == UnitsType.Mortar || cachedUnit.unitData.type == UnitsType.Fregate)
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
	
	/// <summary>
    /// Déclenche le tir de mortier et applique ses dégâts de zone à l'impact.
    /// </summary>
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

	/// <summary>
	/// Boucle d'attaque spécifique aux bateaux qui tirent des projectiles.
	/// </summary>
	private IEnumerator BoatAttackLoopCoroutine()
	{
		while (true)
		{
			if (attackTarget == null) break;

			UnitInstance attackerUnit = cachedUnit;
			if (attackerUnit == null || attackerUnit.currentHealth <= 0) break;

			if (!IsTargetWithinAttackRange()) break;

			UnitInstance targetUnit = attackTarget.GetComponent<UnitInstance>();
			StructureInstance targetStructure = targetUnit == null ? attackTarget.GetComponent<StructureInstance>() : null;

			if (targetUnit == null && targetStructure == null) break;
			if (targetUnit != null && targetUnit.currentHealth <= 0) break;
			if (targetStructure != null && targetStructure.currentHealth <= 0) break;

			float attack = GetAttackDamage(targetUnit, targetStructure);
			float attackSpeed = GetAttackSpeed();

			yield return HandleProjectileAttack(attackerUnit, targetUnit, targetStructure, attack, attackSpeed);
		}

		StopAttackInternal();
	}

	/// <summary>
	/// Récupère le clip d'attaque depuis l'Animator.
	/// </summary>
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

	/// <summary>
	/// Met à jour le booléen d'attaque dans l'Animator.
	/// </summary>
	private void SetAttackAnimationState(bool isAttacking)
	{
		if (animator != null)
			animator.SetBool("isAttacking", isAttacking);
	}

	/// <summary>
	/// Arrête proprement toute attaque en cours et réinitialise les états associés.
	/// </summary>
	private void StopAttackInternal()
	{
		if (attackCoroutine != null)
		{
			StopCoroutine(attackCoroutine);
			attackCoroutine = null;
		}
		SetAttackAnimationState(false);
		isRetaliating = false;

		UnitInstance unit = cachedUnit;
		if (unit != null && unit.objectModel != null)
			unit.objectModel.SetActive(false);
		attackTarget = null;
	}

	/// <summary>
	/// Arrête l'attaque pour reprendre le mouvement.
	/// </summary>
	public void StopAttackForMovement()
	{
		attackTarget = null;
		StopAttackInternal();
	}

	/// <summary>
	/// Gère l'attaque automatique lorsque la cible actuelle reste valide.
	/// </summary>
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
		{
			SetAttackAnimationState(true);

			if (unit != null && unit.objectModel != null)
				unit.objectModel.SetActive(true);
		}
	}

	/// <summary>
	/// Retourne la portée d'attaque courante de l'unité.
	/// </summary>
	private float GetCurrentAttackRange()
	{
		UnitInstance unit = cachedUnit;
		if (unit != null && unit.unitData is UnitCombatData combatData)
			return Mathf.Max(0f, combatData.attackRange);
		return 0.1f;
	}

	/// <summary>
	/// Vérifie si la cible courante est à portée d'attaque.
	/// </summary>
	private bool IsTargetWithinAttackRange()
	{
		if (attackTarget == null)
			return false;

		float attackRange = GetCurrentAttackRange();
		float allowedRange = attackRange + 0.05f;
		return GetFlatDistanceSqToAttackTarget() <= allowedRange * allowedRange;
	}

	/// <summary>
	/// Calcule la distance au carré sur le plan horizontal jusqu'à la cible d'attaque.
	/// </summary>
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

	/// <summary>
	/// Recherche l'instance d'unité ciblée.
	/// </summary>
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

	/// <summary>
	/// Recherche la structure ciblée.
	/// </summary>
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

	/// <summary>
	/// Calcule les dégâts d'attaque en tenant compte des multiplicateurs.
	/// </summary>
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
		return Mathf.Max(0f, damage);
	}

	/// <summary>
	/// Applique les dégâts sur une unité ou structure et déclenche la riposte si nécessaire.
	/// </summary>
	private void ApplyAttackDamage(UnitInstance targetUnit, StructureInstance targetStructure, UnitInstance attackerUnit, float attack)
	{
		if (targetUnit != null)
		{
			targetUnit.TakeDamage(attack, attackerUnit);

			UnitsAnimation targetAnimation = targetUnit.GetComponent<UnitsAnimation>();
			if (targetAnimation != null && attackerUnit != null)
				targetAnimation.AttackTheAttacker(transform);

			return;
		}

		if (targetStructure != null)
			targetStructure.TakeDamage(attack, attackerUnit);
	}

	/// <summary>
	/// Délai / vitesse d'attaque (valeur utilisée pour temporiser les animations / tirs).
	/// </summary>
	private float GetAttackSpeed()
	{
		if (cachedUnit != null && cachedUnit.unitData is UnitCombatData combatData)
			return Mathf.Max(0.1f, combatData.attackSpeed);

		return 1f;
	}

	/// <summary>
	/// Gère l'attaque par projectile : spawn de la cannonball et application des dégâts à l'impact.
	/// </summary>
	private IEnumerator HandleProjectileAttack(UnitInstance attackerUnit, UnitInstance targetUnit, StructureInstance targetStructure, float attack, float attackSpeed)
	{
		float halfDuration = Mathf.Max(0.05f, attackSpeed * 0.5f);

		yield return new WaitForSeconds(halfDuration);

		if (cannonBallPrefab == null || attackTarget == null)
			yield break;

		Transform targetSnapshot = attackTarget;
		UnitInstance attackerSnapshot = attackerUnit;
		Vector3 spawnPos = transform.position;

		CannonBall.Spawn(cannonBallPrefab, spawnPos, targetSnapshot, cannonBallSpeed, (impactPos) =>
		{
			if (targetUnit != null && targetUnit.currentHealth > 0)
			{
				targetUnit.TakeDamage(attack);
				UnitsAnimation anim = targetUnit.GetComponent<UnitsAnimation>();
				if (anim != null)
					anim.AttackTheAttacker(transform);
			}
			else if (targetStructure != null && targetStructure.currentHealth > 0)
			{
				targetStructure.TakeDamage(attack, attackerSnapshot);
			}

		}, transform, 0f, 0.5f);

		yield return new WaitForSeconds(halfDuration);
	}

	/// <summary>
	/// Lance un projectile mortier et applique des dégâts de zone à l'impact.
	/// </summary>
	private void SpawnMortarProjectile(UnitInstance attackerUnit, UnitInstance targetUnit, StructureInstance targetStructure, float attack)
	{
		if (cannonBallPrefab == null || attackTarget == null)
			return;

		Transform targetSnapshot = attackTarget;
		UnitInstance attackerSnapshot = attackerUnit;
		Vector3 spawnPos = transform.position - transform.forward * 0.5f + Vector3.up * 0.5f;
		float impactRadius = 1.5f;

		CannonBall.Spawn(cannonBallPrefab, spawnPos, targetSnapshot, cannonBallSpeed, (impactPos) =>
		{
			GameObject impactZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			impactZone.transform.position = impactPos;
			impactZone.transform.localScale = new Vector3(impactRadius * 2f, 0.05f, impactRadius * 2f);
			impactZone.GetComponent<Collider>().enabled = false;
			impactZone.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/ImpactZone");
			Destroy(impactZone, 1f);

			UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(FindObjectsSortMode.None);
			foreach (UnitInstance hitUnit in allUnits)
			{
				if (hitUnit == null || hitUnit == attackerSnapshot || hitUnit.playerId == attackerSnapshot.playerId || hitUnit.currentHealth <= 0)
					continue;

				float dist = Vector3.Distance(hitUnit.transform.position, impactPos);
				if (dist <= impactRadius)
				{
					float damageMultiplier = 1f + (impactRadius - dist);
					hitUnit.TakeDamage(attack * damageMultiplier);
					UnitsAnimation anim = hitUnit.GetComponent<UnitsAnimation>();
					if (anim != null)
						anim.AttackTheAttacker(transform);
				}
			}

			StructureInstance[] allStructures = FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
			foreach (StructureInstance hitStructure in allStructures)
			{
				if (hitStructure == null || hitStructure.playerId == attackerSnapshot.playerId)
					continue;

				float dist = Vector3.Distance(hitStructure.transform.position, impactPos);
				if (dist <= impactRadius)
					hitStructure.TakeDamage(attack, attackerSnapshot);
			}
		}, transform, 5f);
	}
}


