using UnityEngine;

public class NormalAttack : MonoBehaviour
{
    // L'unité scanne périodiquement autour d'elle pour trouver une structure ennemie.
    [SerializeField, Min(0.1f)] private float scanInterval = 1f;
    [SerializeField, Min(0.1f)] private float scanRadius = 4f;
    [SerializeField, Min(0.1f)] private float stopDistance = 0.5f;
    [SerializeField] private int ownerPlayerId = 1;

    private float scanTimer;
    private StructureInstance currentTarget;
    private MovementManager movementManager;
    private UnitInstance unitInstance;

    private void Awake()
    {
        movementManager = GetComponent<MovementManager>();
        unitInstance = GetComponent<UnitInstance>();
    }

    private void Update()
    {
        if (movementManager == null)
            movementManager = GetComponent<MovementManager>();

        bool isMoving = movementManager != null && movementManager.IsMoving();
        if (scanTimer == 0f)
        {
            Debug.Log($"[NormalAttack] {name} scanning enabled (moving={isMoving}) every {scanInterval}s (baseRadius={scanRadius}) owner={ownerPlayerId}");
        }

        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval)
            return;

        scanTimer = 0f;
        CheckForStructureInRadius();
    }

    private float GetUnitAttackRange()
    {
        if (unitInstance != null && unitInstance.unitData is UnitCombatData combatData)
            return Mathf.Max(0f, combatData.attackRange);
        return 0.1f;
    }

    private bool CanTargetStructures()
    {
        if (unitInstance == null || unitInstance.unitData == null)
            return true;

        return unitInstance.unitData.type != UnitsType.Support && unitInstance.unitData.type != UnitsType.Healer;
    }

    private void CheckForStructureInRadius()
    {
        if (!CanTargetStructures())
        {
            Debug.Log($"[NormalAttack] {name} is Support/Healer -> structure targeting disabled");
            return;
        }

        float effectiveRadius = scanRadius;
        float attackRange = GetUnitAttackRange();
        effectiveRadius = Mathf.Max(effectiveRadius, attackRange + 0.6f);

        Debug.Log($"[NormalAttack] {name} scanning for structures. effectiveRadius={effectiveRadius:F2}, attackRange={attackRange:F2}, owner={ownerPlayerId}");

        StructureInstance chosen = null;
        float bestDistSqr = float.MaxValue;
        float radiusSqr = effectiveRadius * effectiveRadius;
        var structures = Object.FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
        if (structures != null)
        {
            Vector3 origin = transform.position;
            for (int i = 0; i < structures.Length; i++)
            {
                var s = structures[i];
                if (s == null) continue;
                if (s.playerId == ownerPlayerId) continue;
                if (s.structureType == StructureType.Harbour) continue;

                Vector3 pos = s.StructurePosition;
                float dSqr = (pos - origin).sqrMagnitude;
                if (dSqr > radiusSqr) continue;
                if (dSqr < bestDistSqr)
                {
                    bestDistSqr = dSqr;
                    chosen = s;
                }
            }
        }

        if (chosen == null)
        {
            Debug.Log($"[NormalAttack] {name} no enemy non-harbour structure found within {effectiveRadius:F2}");
            return;
        }

        float dist = Vector3.Distance(transform.position, chosen.StructurePosition);
        Debug.Log($"[NormalAttack] {name} found enemy structure '{chosen.name}' at distance={dist:F2} -> ordering move");

        currentTarget = chosen;
        SendAttackOrder(currentTarget);
    }

    private void SendAttackOrder(StructureInstance target)
    {
        if (target == null)
            return;

        MovementManager movement = GetComponent<MovementManager>();
        if (movement != null)
        {
            Debug.Log($"[NormalAttack] {name} sending MoveToTarget order to structure {target.name} at {target.StructurePosition}");
            if (!movement.CanMoveOnWorldPosition(target.StructurePosition))
            {
                Debug.Log($"[NormalAttack] {name} target position not allowed by CanMoveOnWorldPosition, forcing move to {target.StructurePosition}");
                movement.ForceMoveToPosition(target.StructurePosition, stopDistance);
                return;
            }

            movement.MoveToTarget(target.transform, stopDistance);
            return;
        }

        Debug.Log($"[NormalAttack] {name} cible {target.name} mais aucun MovementManager trouvé.");
    }

    public void SetOwnerPlayerId(int playerId)
    {
        ownerPlayerId = playerId;
        Debug.Log($"[NormalAttack] {name} ownerPlayerId set to {ownerPlayerId}");
    }
}