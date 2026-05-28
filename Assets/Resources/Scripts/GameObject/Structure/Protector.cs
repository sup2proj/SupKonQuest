using UnityEngine;

public partial class StructureInstance
{
    /// <summary>
    /// Ajoute une unité protectrice à la liste des protecteurs de la structure.
    /// </summary>
    public void AddProtectorUnit(UnitInstance protectorUnit)
    {
        if (protectorUnit == null)
            return;

        CleanupProtectorUnits();
        GameObject protectorObject = protectorUnit.gameObject;
        if (!unitsProtectorTypes.Contains(protectorObject))
            unitsProtectorTypes.Add(protectorObject);
    }

    /// <summary>
    /// Retire une unité protectrice de la structure.
    /// </summary>
    public void RemoveProtectorUnit(UnitInstance protectorUnit)
    {
        if (protectorUnit == null)
            return;

        unitsProtectorTypes.Remove(protectorUnit.gameObject);
    }

    /// <summary>
    /// Supprime les références nulles dans la liste des protecteurs.
    /// </summary>
    private void CleanupProtectorUnits()
    {
        unitsProtectorTypes.RemoveAll(unitObject => unitObject == null);
    }

    /// <summary>
    /// Applique des dégâts à la structure et déclenche les réactions défensives si nécessaire.
    /// </summary>
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

        if (IAInstance.IsDifficultyForPlayer(playerId, 2) && attacker != null && attacker.playerId != playerId)
        {
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

    /// <summary>
    /// Réagit à la mort d'une unité protectrice liée à la structure.
    /// </summary>
    public void HandleProtectorDeath(UnitInstance protectorUnit, int killerPlayerId)
    {
        if (protectorUnit != null)
            RemoveProtectorUnit(protectorUnit);

        if (killerPlayerId <= 0 || killerPlayerId == playerId)
            return;

        int previousOwnerId = playerId;
        CaptureStructure(killerPlayerId, previousOwnerId, checkDefeat: true);
    }

    /// <summary>
    /// Demande aux unités protectrices de riposter contre l'attaquant.
    /// </summary>
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
