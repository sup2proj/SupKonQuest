using UnityEngine;
using System.Collections;

public partial class UnitsAnimation : MonoBehaviour
{
    public void StartAttackAnimationFromSpell()
    {
        UnitInstance unit = cachedUnit != null ? cachedUnit : GetComponent<UnitInstance>();
        if (animator != null)
            animator.SetBool("isAttacking", true);
        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(true);
        spellAttackResetCoroutine = StartCoroutine(ResetSpellAttackToIdleAfterAnimation());
    }

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