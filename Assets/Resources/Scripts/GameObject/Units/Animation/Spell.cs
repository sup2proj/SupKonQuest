
using UnityEngine;
using System.Collections;

/// <summary>
/// Méthodes liées aux spells / effets spéciaux lançant des animations d'attaque.
/// Partie spells de la classe partielle `UnitsAnimation`.
/// </summary>
public partial class UnitsAnimation : MonoBehaviour
{
	/// <summary>
	/// Lance l'animation d'attaque déclenchée par un sort (sans appliquer de dégâts).
	/// </summary>
	public void StartAttackAnimationFromSpell()
	{
		SetAttackVisuals(true);
		StartCoroutine(ResetSpellAttackToIdleAfterAnimation());
	}

	/// <summary>
	/// Réinitialise l'état d'attaque au retour à l'état idle après la durée du clip d'attaque.
	/// </summary>
	private IEnumerator ResetSpellAttackToIdleAfterAnimation()
	{
		AnimationClip attackClip = GetAttackClip();
		float duration = attackClip != null ? attackClip.length : 0.05f;
		duration = Mathf.Max(0.05f, duration);

		yield return new WaitForSeconds(duration);

		SetAttackVisuals(false);
	}
}


