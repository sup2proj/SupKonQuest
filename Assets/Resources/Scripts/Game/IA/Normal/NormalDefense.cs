// Si l'ia est attaqué alors elle essaie de créer des unités qui défendent sa structure

// Le pourcentage de création d'unité protectrice augmente considérablement

// Et on créer des unités uniquement dans la structure attaqué 

// Il suffit qu'elle recoit un seul dégat pour déclencher plein de création d'unité dans la structure

// L'ia va envoyer va avoir un rayon dans laquelle on va rappeler toutes les unités pour attaquer l'unité qui attaque. On se servira des methodes qu'il y a dans unitsregistry et structure manager

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// Récupère la référence à la <see cref="StructureInstance"/> associée.
    /// </summary>
    private void Awake()
    {
        structureInstance = GetComponent<StructureInstance>();
    }

    /// <summary>
    /// Appelée lorsque la structure est attaquée : prépare la défense, peut
    /// enqueuer des protecteurs et rappeler des unités locales pour contrer
    /// l'attaquant fourni.
    /// </summary>
    /// <param name="attacker">Instance de l'unité attaquante.</param>
    public void MyStructureAttacked(UnitInstance attacker)
    {
        if (structureInstance == null || attacker == null)
            return;

        if (!IAInstance.IsDifficultyForPlayer(structureInstance.playerId, 2))
            return;

        Debug.Log($"[NormalDefense] {structureInstance.name} attacked by {attacker.name} (player {attacker.playerId}). Checking allies in radius...");

        lastAttacker = attacker;
        defendUntilTime = Time.time + defendDuration;
        isDefending = true;

        if (HasCombatAllyNearby())
        {
            Debug.Log($"[NormalDefense] Combat ally found near {structureInstance.name} - creating protectors.");
            CreateProtectorsInAttackedStructure();
        }
        else
        {
            Debug.Log($"[NormalDefense] No combat ally near {structureInstance.name} - no protectors created.");
        }

        CallUnitsInRadiusForProtect();
    }

    /// <summary>
    /// Appelée pour indiquer que la période de défense est terminée. Réinitialise
    /// l'état si le délai de défense est écoulé.
    /// </summary>
    public void MyStructureDefended()
    {
        if (!isDefending)
            return;

        if (Time.time < defendUntilTime)
            return;

        isDefending = false;
        lastAttacker = null;
    }

    /// <summary>
    /// Vérifie s'il existe des unités alliées de type combat à proximité de
    /// la structure (selon le rayon configuré par la structure).
    /// </summary>
    /// <returns>True si une unité de combat alliée est trouvée.</returns>
    private bool HasCombatAllyNearby()
    {
        if (structureInstance == null)
            return false;

        var nearbyUnits = structureInstance.GetUnitsWithinConfiguredRadius();
        for (int i = 0; i < nearbyUnits.Count; i++)
        {
            UnitInstance unit = nearbyUnits[i];
            if (unit == null || unit.unitData == null)
                continue;

            if (unit.playerId != structureInstance.playerId)
                continue;

            if (unit.unitData is UnitCombatData)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Rappelle les unités alliées dans un rayon autour de la structure afin
    /// qu'elles engagent la dernière unité attaquante.
    /// </summary>
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

    /// <summary>
    /// Enfile des unités protectrices dans la file de production de la structure
    /// attaquée en se basant sur les unités proches et des règles de fallback.
    /// </summary>
    private void CreateProtectorsInAttackedStructure()
    {
        if (structureInstance == null || StructureManager.Instance == null)
            return;

        var nearbyUnits = structureInstance.GetUnitsWithinConfiguredRadius();
        HashSet<UnitsType> nearbyTypes = new HashSet<UnitsType>();
        for (int i = 0; i < nearbyUnits.Count; i++)
        {
            var u = nearbyUnits[i];
            if (u == null || u.unitData == null) continue;
            if (u.playerId != structureInstance.playerId) continue;
            nearbyTypes.Add(u.unitData.type);
        }

        string typesStr = nearbyTypes.Count > 0 ? string.Join(",", nearbyTypes.Select(t => t.ToString())) : "<none>";
        Debug.Log($"[NormalDefense] Nearby ally types for {structureInstance.name}: {typesStr}");

        var allUnitDatas = StructureManager.Instance.unitData;
        if (allUnitDatas == null || allUnitDatas.Count == 0)
        {
            Debug.LogWarning($"[NormalDefense] {structureInstance.name} No unitData configured in StructureManager.");
            return;
        }

        // Première passe: sélectionner les types qui sont présents à proximité (préférence)
        List<UnitData> candidates = new List<UnitData>();
        for (int i = 0; i < allUnitDatas.Count; i++)
        {
            UnitData data = allUnitDatas[i];
            if (data == null) continue;
            if (nearbyTypes.Contains(data.type) && (data is UnitCombatData) && data.type != UnitsType.Support && data.type != UnitsType.Healer)
            {
                candidates.Add(data);
            }
        }

        // Si aucun candidat correspondant aux types proches, fallback sur les unitDatas marquées isProtector
        if (candidates.Count == 0)
        {
            for (int i = 0; i < allUnitDatas.Count; i++)
            {
                UnitData data = allUnitDatas[i];
                if (data == null) continue;
                if (data.isProtector)
                    candidates.Add(data);
            }
        }

        // Dernier fallback: toutes les unités de combat disponibles
        if (candidates.Count == 0)
        {
            for (int i = 0; i < allUnitDatas.Count; i++)
            {
                UnitData data = allUnitDatas[i];
                if (data == null) continue;
                if (data.type == UnitsType.Support || data.type == UnitsType.Healer) continue;
                if (data.type == UnitsType.Fregate || data.type == UnitsType.Destroyer || data.type == UnitsType.Transport) continue;
                if (data is UnitCombatData)
                    candidates.Add(data);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[NormalDefense] {structureInstance.name} no valid protector candidates found.");
            return;
        }

        int spawnCount = Mathf.Max(1, Mathf.CeilToInt(protectSpawnMultiplier));
        Debug.Log($"[NormalDefense] {structureInstance.name} will enqueue {spawnCount} protectors (candidates={candidates.Count}).");
        for (int i = 0; i < spawnCount; i++)
        {
            UnitData chosen = candidates[Random.Range(0, candidates.Count)];
            structureInstance.AddToQueue(chosen.type, true);
            Debug.Log($"[NormalDefense] enqueued protector {chosen.type} in structure {structureInstance.name} (isProtector=true)");
        }
    }

    /// <summary>
    /// Force le spawn immédiat d'une unité protectrice pour débogage ou
    /// situation d'urgence (choisit un type de combat disponible).
    /// </summary>
    public void ForceSpawnProtectorNow()
    {
        if (structureInstance == null || StructureManager.Instance == null)
        {
            Debug.LogWarning("[NormalDefense] Cannot force spawn: missing references.");
            return;
        }

        var allUnitDatas = StructureManager.Instance.unitData;
        if (allUnitDatas == null || allUnitDatas.Count == 0)
        {
            Debug.LogWarning("[NormalDefense] No unitData available to spawn.");
            return;
        }

        // pick any combat unit as protector for debug
        UnitData chosen = allUnitDatas.FirstOrDefault(d => d != null && d is UnitCombatData && d.type != UnitsType.Support && d.type != UnitsType.Healer);
        if (chosen == null)
        {
            chosen = allUnitDatas.FirstOrDefault(d => d != null);
        }

        if (chosen == null)
        {
            Debug.LogWarning("[NormalDefense] No candidate found for forced spawn.");
            return;
        }

        Vector3 pos = structureInstance != null ? structureInstance.StructurePosition : transform.position;
        bool spawned = StructureManager.Instance.SpawnUnitByTypeAtPosition(structureInstance.playerId, chosen.type, pos.x, pos.z, false, true, structureInstance);
        Debug.Log($"[NormalDefense] ForceSpawnProtectorNow: tried to spawn {chosen.type} -> {(spawned ? "OK" : "FAILED")}");
    }
}
