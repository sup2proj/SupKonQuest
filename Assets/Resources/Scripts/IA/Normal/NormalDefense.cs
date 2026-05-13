// Si l'ia est attaqué alors elle essaie de créer des unités qui défendent sa structure

// Le pourcentage de création d'unité protectrice augmente considérablement

// Et on créer des unités uniquement dans la structure attaqué 

// Il suffit qu'elle recoit un seul dégat pour déclencher plein de création d'unité dans la structure

// L'ia va envoyer va avoir un rayon dans laquelle on va rappeler toutes les unités pour attaquer l'unité qui attaque. On se servira des methodes qu'il y a dans unitsregistry et structure manager

using UnityEngine;
using System.Collections.Generic;

public class NormalDefense : MonoBehaviour
{
    [Header("Defense Settings")]
    [SerializeField, Min(0.1f)] private float defendDuration = 8f;
    [SerializeField, Min(0.1f)] private float protectSpawnMultiplier = 4f;
    [SerializeField, Min(0.1f)] private float callUnitsRadius = 10f;

    private StructureInstance structureInstance;
    private UnitInstance lastAttacker;
    private float defendUntilTime;
    private bool isDefending;
    private bool isTriggered;

    private float originalSpawnChanceBackup = -1f;
    private float nextForcedSpawnTime;
    private int forcedSpawnBudget;

    private void Awake()
    {
        structureInstance = GetComponent<StructureInstance>();
    }

    public void MyStructureAttacked(UnitInstance attacker)
    {
        if (structureInstance == null || attacker == null)
            return;

        // Ne déclencher la défense que si l'IA globale est en difficulty 2
        var ia = FindFirstObjectByType<IAInstance>();
        if (ia == null || ia.DifficultyIA != 2)
            return;

        // Vérifier que la structure appartient bien à l'IA (test setup uses playerId==1)
        if (structureInstance.playerId != 1)
            return;

        lastAttacker = attacker;
        defendUntilTime = Time.time + defendDuration;

        // Premier dégât: on initialise l'état de défense + burst protector.
        if (!isTriggered)
        {
            isTriggered = true;
            IncreaseUnitsProtectorSpawnRate();
            CreateUnitsInOnlyAttackedStructure();
        }

        // à chaque dégât reçu, on rappelle les unités proches
        // vers l'attaquant actuel.
        CallUnitsInRadiusForProtect();

        isDefending = true;
    }


    public void MyStructureDefended()
    {
        if (!isTriggered)
            return;

        if (Time.time < defendUntilTime)
            return;

        isTriggered = false;
        isDefending = false;
        lastAttacker = null;
        DecreaseUnitsProtectorSpawnRate();
    }

    private void IncreaseUnitsProtectorSpawnRate()
    {
        if (structureInstance == null || StructureManager.Instance == null)
            return;

        forcedSpawnBudget = Mathf.Max(3, Mathf.RoundToInt(protectSpawnMultiplier));
        nextForcedSpawnTime = Time.time;
        Debug.Log($"[NormalDefense] {structureInstance.name} defense triggered: increasing protector production rate x{protectSpawnMultiplier}, budget={forcedSpawnBudget}");
    }

    private void DecreaseUnitsProtectorSpawnRate()
    {
        if (structureInstance == null)
            return;

        forcedSpawnBudget = 0;
        Debug.Log($"[NormalDefense] {structureInstance.name} defense ended: restoring protector production rate");
    }

    private void CallUnitsInRadiusForProtect()
    {
        if (structureInstance == null || lastAttacker == null)
            return;

        Vector3 center = structureInstance.StructurePosition;
        float radiusSqr = callUnitsRadius * callUnitsRadius;
        List<UnitInstance> snapshot = UnitsRegistry.GetSnapshot();

        for (int i = 0; i < snapshot.Count; i++)
        {
            UnitInstance unit = snapshot[i];
            if (unit == null || unit.unitData == null)
                continue;
            if (unit.playerId != structureInstance.playerId)
                continue;
            if (unit.unitData.type == UnitsType.Support || unit.unitData.type == UnitsType.Healer)
                continue;

            float distSqr = (unit.transform.position - center).sqrMagnitude;
            if (distSqr > radiusSqr)
                continue;

            UnitsAnimation anim = unit.GetComponent<UnitsAnimation>();
            if (anim == null)
                continue;

            float stopDistance = 0.1f;
            if (unit.unitData is UnitCombatData combatData)
                stopDistance = Mathf.Max(0f, combatData.attackRange);

            Debug.Log($"[NormalDefense] recalling unit {unit.name} to attack {lastAttacker.name} (dist={Mathf.Sqrt(distSqr):F2})");
            anim.EngageTarget(lastAttacker.transform, stopDistance);
        }
    }

    private void CreateUnitsInOnlyAttackedStructure()
    {
        if (structureInstance == null || StructureManager.Instance == null)
            return;

        // Récupérer les unitDatas configurées globalement
        List<UnitData> candidates = new List<UnitData>();
        var allUnitDatas = StructureManager.Instance.unitData;
        if (allUnitDatas == null || allUnitDatas.Count == 0)
        {
            Debug.LogWarning($"[NormalDefense] {structureInstance.name} No unitData configured in StructureManager.");
            return;
        }

        // 1) Priorité aux unités explicitement marquées protectrices
        for (int i = 0; i < allUnitDatas.Count; i++)
        {
            UnitData data = allUnitDatas[i];
            if (data == null) continue;
            if (data.isProtector)
                candidates.Add(data);
        }

        // 2) Fallback si aucun protecteur configuré: on prend les unités de combat non-support/non-healer
        if (candidates.Count == 0)
        {
            for (int i = 0; i < allUnitDatas.Count; i++)
            {
                UnitData data = allUnitDatas[i];
                if (data == null) continue;
                if (data.type == UnitsType.Support || data.type == UnitsType.Healer) continue;
                // Ignorer types bateau
                if (data.type == UnitsType.Fregate || data.type == UnitsType.Destroyer || data.type == UnitsType.Transport) continue;
                // Prioriser les unités de combat
                if (data is UnitCombatData)
                    candidates.Add(data);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[NormalDefense] {structureInstance.name} no valid protector candidates found.");
            return;
        }

        int enqueueCount = Mathf.Max(1, Mathf.CeilToInt(protectSpawnMultiplier));
        Vector3 pos = structureInstance.StructurePosition;

        for (int i = 0; i < enqueueCount; i++)
        {
            UnitData chosen = candidates[Random.Range(0, candidates.Count)];
            // Enqueue into the structure's production queue so the unit is created like a normal queued unit
            structureInstance.AddToQueue(chosen.type);
            Debug.Log($"[NormalDefense] enqueued protector {chosen.type} in structure {structureInstance.name} (isProtector=true)");
        }
    }
}
