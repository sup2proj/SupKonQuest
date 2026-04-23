using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Buffs
{
    public static void ApplyRegen(
        MonoBehaviour owner,
        Dictionary<UnitInstance, Coroutine> regenByTarget,
        UnitInstance target,
        UnitHealerData healerData,
        float regenTickInterval,
        int sourcePlayerId)
    {
        if (target == null || target.playerId != sourcePlayerId)
            return;

        if (regenByTarget.TryGetValue(target, out Coroutine running) && running != null)
        {
            owner.StopCoroutine(running);
            regenByTarget.Remove(target);
        }

        Coroutine c = owner.StartCoroutine(RegenCoroutine(target, healerData.healAmount, healerData.healDuration, regenTickInterval, sourcePlayerId, regenByTarget));
        regenByTarget[target] = c;
    }

    private static IEnumerator RegenCoroutine(
        UnitInstance target,
        float totalHealAmount,
        float duration,
        float regenTickInterval,
        int sourcePlayerId,
        Dictionary<UnitInstance, Coroutine> regenByTarget)
    {
        float tick = Mathf.Max(0.01f, regenTickInterval);
        float elapsed = 0f;
        float healPerSecond = totalHealAmount / Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (target == null || target.playerId != sourcePlayerId)
                break;

            float dt = Mathf.Min(tick, duration - elapsed);
            float healThisTick = healPerSecond * dt;
            target.SetHealth(healThisTick);

            elapsed += dt;
            yield return new WaitForSeconds(dt);
        }

        regenByTarget.Remove(target);
    }

    public static void BuffStatistics(
        MonoBehaviour owner,
        Dictionary<UnitInstance, Coroutine> buffByTarget,
        UnitInstance target,
        UnitSupportData supportData,
        float buffMultiplicator,
        int spell,
        int sourcePlayerId)
    {
        if (owner == null || buffByTarget == null || target == null || target.unitData == null || supportData == null)
            return;

        if (target.playerId != sourcePlayerId)
            return;

        if (buffMultiplicator <= 0f)
            return;

        // Vérifier que la cible a les stats nécessaires
        // spell 2 = attackSpeed (nécessite UnitCombatData)
        // spell 3 = speed (disponible pour tous)
        // spell 4 = damage (nécessite UnitCombatData)
        if ((spell == 2 || spell == 4) && !(target.unitData is UnitCombatData))
        {
            Debug.LogWarning($"[Buffs] Impossible d'appliquer le buff {spell} à {target.name}: unitData n'est pas UnitCombatData");
            return;
        }

        if (buffByTarget.TryGetValue(target, out Coroutine running) && running != null)
        {
            owner.StopCoroutine(running);
            buffByTarget.Remove(target);
        }

        Coroutine c = owner.StartCoroutine(BuffCoroutine(target, supportData.buffDuration, buffMultiplicator, spell, sourcePlayerId, buffByTarget));
        buffByTarget[target] = c;
    }

    private static IEnumerator BuffCoroutine(
        UnitInstance target,
        float duration,
        float buffMultiplicator,
        int spell,
        int sourcePlayerId,
        Dictionary<UnitInstance, Coroutine> buffByTarget)
    {
        if (target == null || target.unitData == null || target.playerId != sourcePlayerId)
            yield break;
        
        float originalValue = 0f;
        if (!TryGetSpellStat(target, spell, out originalValue))
            yield break;
        
        float buffedValue = originalValue * buffMultiplicator;
        if (!TrySetSpellStat(target, spell, buffedValue))
            yield break;

        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        if (target != null)
            TrySetSpellStat(target, spell, originalValue);

        buffByTarget.Remove(target);
    }

    private static bool TryGetSpellStat(UnitInstance target, int spell, out float value)
    {
        value = 0f;
        if (target == null || target.unitData == null)
            return false;

        switch (spell)
        {
            case 2:
            case 4:
                if (target.unitData is UnitCombatData combatData)
                {
                    value = (spell == 2) ? combatData.attackSpeed : combatData.attack;
                    return true;
                }
                return false;
            case 3:
                value = target.unitData.speed;
                return true;
            default:
                return false;
        }
    }

    private static bool TrySetSpellStat(UnitInstance target, int spell, float value)
    {
        switch (spell)
        {
            case 2:
                if (target.unitData is UnitCombatData combatDataForSpeed)
                {
                    combatDataForSpeed.attackSpeed = Mathf.Max(0f, value);
                    return true;
                }
                return false;
            case 3:
                target.unitData.speed = Mathf.Max(0f, value);
                return true;
            case 4:
                if (target.unitData is UnitCombatData combatDataForDamage)
                {
                    combatDataForDamage.attack = Mathf.Max(0f, value);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }
}