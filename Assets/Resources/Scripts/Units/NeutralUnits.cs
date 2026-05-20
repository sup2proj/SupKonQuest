using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitInstance))]
public class NeutralUnits : MonoBehaviour
{
    [Header("Neutral behavior")]
    [SerializeField, Min(0.2f)] private float detectionRadius = 4f;
    [SerializeField, Min(0.05f)] private float scanInterval = 0.2f;
    [SerializeField, Min(0f)] private float stopDistance = 0.5f;

    private UnitInstance unit;
    private UnitsAnimation unitsAnimation;
    private MovementManager movementManager;
    private float nextScanAt;
    private bool visualsApplied;

    public static void ConvertPlayerUnitsToNeutral(int defeatedPlayerId, bool removeAiRandomMovement)
    {
        if (defeatedPlayerId <= 0)
            return;

        List<UnitInstance> snapshot = UnitsRegistry.GetSnapshot();
        for (int i = 0; i < snapshot.Count; i++)
        {
            UnitInstance candidate = snapshot[i];
            if (candidate == null || candidate.playerId != defeatedPlayerId)
                continue;

            candidate.SetNeutralState(true);

            NeutralUnits neutral = candidate.GetComponent<NeutralUnits>();
            if (neutral == null)
                neutral = candidate.gameObject.AddComponent<NeutralUnits>();

            neutral.ApplyNeutralSetup();

            if (removeAiRandomMovement)
            {
                MovementEasyNormal movementEasyNormal = candidate.GetComponent<MovementEasyNormal>();
                if (movementEasyNormal != null)
                    Destroy(movementEasyNormal);

                EasyMovement easyMovement = candidate.GetComponent<EasyMovement>();
                if (easyMovement != null)
                    Destroy(easyMovement);
            }
        }
    }

    private void Awake()
    {
        unit = GetComponent<UnitInstance>();
        unitsAnimation = GetComponent<UnitsAnimation>();
        movementManager = GetComponent<MovementManager>();
    }

    private void OnEnable()
    {
        if (unit != null && unit.isNeutral)
            ApplyNeutralSetup();
    }

    private void Update()
    {
        if (unit == null || !unit.isNeutral || unit.currentHealth <= 0f)
            return;

        if (!CanAttackWithDamage())
        {
            if (movementManager != null)
                movementManager.StopMovement();
            return;
        }

        if (Time.time < nextScanAt)
            return;

        nextScanAt = Time.time + scanInterval;
        UnitInstance enemy = FindNearestEnemyInRadius();
        if (enemy == null)
        {
            if (unitsAnimation != null && unitsAnimation.AttackTarget == null && movementManager != null)
                movementManager.StopMovement();
            return;
        }

        float engageDistance = Mathf.Max(stopDistance, GetAttackRange());
        if (unitsAnimation != null)
            unitsAnimation.EngageTarget(enemy.transform, engageDistance);
        else if (movementManager != null)
            movementManager.MoveToTarget(enemy.transform, engageDistance);
    }

    private void ApplyNeutralSetup()
    {
        if (unit == null)
            return;

        unit.SetNeutralState(true);
        ApplyNeutralVisuals();

        if (movementManager == null)
            movementManager = GetComponent<MovementManager>();

        if (movementManager != null)
            movementManager.StopMovement();
    }

    private bool CanAttackWithDamage()
    {
        if (unit == null || unit.unitData == null)
            return false;

        UnitsType type = unit.unitData.type;
        return type != UnitsType.Support && type != UnitsType.Healer;
    }

    private float GetAttackRange()
    {
        if (unit != null && unit.unitData is UnitCombatData combatData)
            return Mathf.Max(0f, combatData.attackRange);

        return 0.1f;
    }

    private UnitInstance FindNearestEnemyInRadius()
    {
        List<UnitInstance> units = UnitsRegistry.GetSnapshot();
        Vector3 origin = transform.position;
        float radiusSq = detectionRadius * detectionRadius;
        UnitInstance best = null;
        float bestSq = float.MaxValue;

        for (int i = 0; i < units.Count; i++)
        {
            UnitInstance candidate = units[i];
            if (candidate == null || candidate == unit || candidate.currentHealth <= 0f)
                continue;

            if (candidate.playerId == unit.playerId)
                continue;

            // Neutral units cannot attack protector units
            if (candidate.unitData != null && candidate.unitData.isProtector)
                continue;

            Vector3 delta = candidate.transform.position - origin;
            delta.y = 0f;
            float sq = delta.sqrMagnitude;
            if (sq > radiusSq || sq >= bestSq)
                continue;

            bestSq = sq;
            best = candidate;
        }

        return best;
    }

    private void ApplyNeutralVisuals()
    {
        if (visualsApplied)
            return;

        SpriteRenderer circleRenderer = unit != null && unit.circleUnderFeet != null
            ? unit.circleUnderFeet.GetComponent<SpriteRenderer>()
            : null;
        if (circleRenderer != null)
        {
            Color c = circleRenderer.color;
            circleRenderer.color = new Color(0f, 0f, 0f, c.a);
        }

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null)
                continue;

            Color c = sr.color;
            sr.color = new Color(0f, 0f, 0f, c.a);
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);

            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", Color.black);
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                block.SetColor("_Color", Color.black);

            r.SetPropertyBlock(block);
        }

        visualsApplied = true;
    }
}