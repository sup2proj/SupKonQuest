using UnityEngine;

public partial class StructureInstance
{
    public void AddProtectorUnit(UnitInstance protectorUnit)
    {
        if (protectorUnit == null)
            return;

        CleanupProtectorUnits();
        GameObject protectorObject = protectorUnit.gameObject;
        if (!unitsProtectorTypes.Contains(protectorObject))
            unitsProtectorTypes.Add(protectorObject);
    }

    public void RemoveProtectorUnit(UnitInstance protectorUnit)
    {
        if (protectorUnit == null)
            return;

        unitsProtectorTypes.Remove(protectorUnit.gameObject);
    }

    private void CleanupProtectorUnits()
    {
        unitsProtectorTypes.RemoveAll(unitObject => unitObject == null);
    }

    public void TakeDamage(float amount, UnitInstance attacker)
    {
        if (attacker != null && attacker.playerId == playerId)
            return;

        int previousOwnerId = playerId;

        currentHealth -= Mathf.RoundToInt(amount);
        currentHealth = Mathf.Clamp(currentHealth, 0, health);
    
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0 && attacker != null && attacker.playerId != previousOwnerId)
        {
            CaptureStructure(attacker.playerId, previousOwnerId);
            return;
        }

        // DÃ©clenchement IA uniquement si le comportement IA niveau 2 est actif
        if (IAInstance.IsDifficultyForPlayer(playerId, 2) && attacker != null && attacker.playerId != playerId)
        {
            // On regarde dans le rayon de la structure: s'il y a au moins une unitÃ© de combat alliÃ©e,
            // on autorise la crÃ©ation de protecteurs.
            bool hasCombatAllyNearby = false;
            var nearbyUnits = GetUnitsWithinConfiguredRadius();
            for (int i = 0; i < nearbyUnits.Count; i++)
            {
                UnitInstance unit = nearbyUnits[i];
                if (unit == null || unit.unitData == null)
                    continue;

                if (unit.playerId != playerId)
                    continue;

                if (unit.unitData is UnitCombatData)
                {
                    hasCombatAllyNearby = true;
                    break;
                }
            }

            if (hasCombatAllyNearby)
            {
                NormalDefense normalDefense = GetComponent<NormalDefense>();
                if (normalDefense == null)
                    normalDefense = gameObject.AddComponent<NormalDefense>();

                normalDefense.MyStructureAttacked(attacker);
            }
        }

        TryTriggerProtectorRetaliation(attacker);
        StructureAttack structureAttack = GetComponent<StructureAttack>();
        if (structureAttack != null)
            structureAttack.OnAttacked(attacker);
    }

    public void HandleProtectorDeath(UnitInstance protectorUnit, int killerPlayerId)
    {
        if (protectorUnit != null)
            RemoveProtectorUnit(protectorUnit);

        if (killerPlayerId <= 0 || killerPlayerId == playerId)
            return;

        int previousOwnerId = playerId;
        // Important: la capture par mort du protecteur doit aussi vÃ©rifier dÃ©faite/victoire.
        CaptureStructure(killerPlayerId, previousOwnerId, checkDefeat: true);
    }

    private void TryTriggerProtectorRetaliation(UnitInstance attacker)
    {
        if (attacker == null || attacker.transform == null)
            return;
        if (attacker.playerId == playerId)
            return;

        CleanupProtectorUnits();

        for (int i = unitsProtectorTypes.Count - 1; i >= 0; i--)
        {
            GameObject protectorObject = unitsProtectorTypes[i];
            if (protectorObject == null)
            {
                unitsProtectorTypes.RemoveAt(i);
                continue;
            }

            UnitInstance protector = protectorObject.GetComponent<UnitInstance>();
            if (protector == null || protector.unitData == null || !protector.unitData.isProtector)
            {
                unitsProtectorTypes.RemoveAt(i);
                continue;
            }

            if (protector.playerId != playerId)
                continue;

            UnitsAnimation protectorAnimation = protectorObject.GetComponent<UnitsAnimation>();
            if (protectorAnimation == null)
                continue;

            float stopDistance = 0.1f;
            if (protector.unitData is UnitCombatData combatData)
                stopDistance = Mathf.Max(0f, combatData.attackRange);

            protectorAnimation.EngageTarget(attacker.transform, stopDistance);
        }
    }
}
