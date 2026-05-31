
using UnityEngine;
using System.Collections;

/// <summary>
/// Méthodes liées au mouvement et à l'engagement sur une cible.
/// Partie mouvement de la classe partielle `UnitsAnimation`.
/// </summary>
public partial class UnitsAnimation : MonoBehaviour
{
	/// <summary>
	/// Appelé lorsque le MovementManager signale la fin du déplacement vers une cible.
	/// Définit la cible d'attaque et lance l'attaque si possible.
	/// </summary>
	private void OnMovementCompleted(Transform target)
	{
		if (target == null)
			return;

		attackTarget = target;
		if (CanAttackWithDamage(cachedUnit))
			StartAttackWithDamage();
	}

	/// <summary>
	/// Demande au MovementManager de se déplacer vers la cible en conservant une distance d'arrêt.
	/// </summary>
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

	/// <summary>
	/// Active le modèle d'objet pour l'archer lorsqu'il se déplace (visuel spécifique).
	/// </summary>
	private void RefreshMovingArcherModel()
	{
		UnitInstance unit = cachedUnit;
		if (animator == null || movementManager == null || unit == null || unit.objectModel == null || unit.unitData == null)
			return;

		if (unit.unitData.type == UnitsType.Archer && movementManager.IsMoving())
			unit.objectModel.SetActive(true);
	}
}


