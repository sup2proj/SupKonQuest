using UnityEngine;
using System.Collections;

public partial class UnitsAnimation : MonoBehaviour
{
    /// <summary>
    /// Déclenche l'animation d'attaque utilisée par un sort.
    /// </summary>
    public void StartAttackAnimationFromSpell()
    {
        UnitInstance unit = cachedUnit != null ? cachedUnit : GetComponent<UnitInstance>();
        if (animator != null)
            animator.SetBool("isAttacking", true);
        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(true);
        spellAttackResetCoroutine = StartCoroutine(ResetSpellAttackToIdleAfterAnimation());
    }

    /// <summary>
    /// Rétablit l'état idle après la fin de l'animation d'attaque déclenchée par un sort.
    /// </summary>
    private IEnumerator ResetSpellAttackToIdleAfterAnimation()
    {
        AnimationClip attackClip = GetAttackClip();
        float duration = attackClip != null ? attackClip.length : 0.05f;
        duration = Mathf.Max(0.05f, duration);
        yield return new WaitForSeconds(duration);
        if (animator != null)
            animator.SetBool("isAttacking", false);
        UnitInstance unit = cachedUnit;
        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(false);
        spellAttackResetCoroutine = null;
    }
}