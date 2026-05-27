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
    private UnitsAnimation unitsAnimation;
    private UnitInstance unitInstance;

    /// <summary>
    /// Initialise les références aux composants nécessaires (movement, animation, unit).
    /// </summary>
    private void Awake()
    {
        movementManager = GetComponent<MovementManager>();
        unitsAnimation = GetComponent<UnitsAnimation>();
        unitInstance = GetComponent<UnitInstance>();
    }

    /// <summary>
    /// Effectue un scan périodique autour de l'unité pour détecter des structures
    /// ennemies et lancer des ordres d'attaque si nécessaire.
    /// </summary>
    private void Update()
    {
        if (movementManager == null)
            movementManager = GetComponent<MovementManager>();
        if (unitsAnimation == null)
            unitsAnimation = GetComponent<UnitsAnimation>();

        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval)
            return;

        scanTimer = 0f;
        CheckForStructureInRadius();
    }

    /// <summary>
    /// Retourne la portée d'attaque de l'unité si elle est de type combat;
    /// sinon retourne une petite valeur par défaut.
    /// </summary>
    /// <returns>Portée d'attaque en unités de distance.</returns>
    private float GetUnitAttackRange()
    {
        if (unitInstance != null && unitInstance.unitData is UnitCombatData combatData)
            return Mathf.Max(0f, combatData.attackRange);
        return 0.1f;
    }

    /// <summary>
    /// Indique si l'unité peut cibler des structures (les unités de support/healer
    /// ne ciblent pas les structures).
    /// </summary>
    /// <returns>True si l'unité peut cibler des structures.</returns>
    private bool CanTargetStructures()
    {
        if (unitInstance == null || unitInstance.unitData == null)
            return true;

        return unitInstance.unitData.type != UnitsType.Support && unitInstance.unitData.type != UnitsType.Healer;
    }

    /// <summary>
    /// Parcourt les structures à proximité et choisit la plus proche
    /// qui appartient à un joueur ennemi. Lance ensuite l'ordre d'attaque.
    /// </summary>
    private void CheckForStructureInRadius()
    {
        if (!CanTargetStructures())
        {
            return;
        }

        float effectiveRadius = scanRadius;
        float attackRange = GetUnitAttackRange();
        effectiveRadius = Mathf.Max(effectiveRadius, attackRange + 0.6f);

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
            return;
        }

        float dist = Vector3.Distance(transform.position, chosen.StructurePosition);

        currentTarget = chosen;
        SendAttackOrder(currentTarget);
    }

    /// <summary>
    /// Envoie l'ordre d'attaque vers la <paramref name="target"/> en prenant en
    /// compte la portée d'attaque, l'animation et la navigabilité de la position.
    /// </summary>
    /// <param name="target">Structure à attaquer.</param>
    private void SendAttackOrder(StructureInstance target)
    {
        if (target == null)
            return;

        MovementManager movement = GetComponent<MovementManager>();
        float attackStopDistance = Mathf.Max(stopDistance, GetUnitAttackRange());
        if (unitsAnimation != null)
        {
            if (unitsAnimation.TryStartAttackTargetIfInRange(target.transform))
                return;

            if (movement != null && !movement.CanMoveOnWorldPosition(target.StructurePosition))
            {
                movement.ForceMoveToPosition(target.StructurePosition, attackStopDistance);
                return;
            }

            if (movement != null)
                movement.MoveToTarget(target.transform, attackStopDistance);<
            return;
        }

        if (movement != null)
        {
            if (!movement.CanMoveOnWorldPosition(target.StructurePosition))
            {
                movement.ForceMoveToPosition(target.StructurePosition, attackStopDistance);
                return;
            }

            movement.MoveToTarget(target.transform, attackStopDistance);
            return;
        }
    }

    /// <summary>
    /// Définit l'identifiant du joueur propriétaire de cette unité (utilisé pour
    /// déterminer quels targets sont ennemis).
    /// </summary>
    /// <param name="playerId">Identifiant du joueur propriétaire.</param>
    public void SetOwnerPlayerId(int playerId)
    {
        ownerPlayerId = playerId;
    }
}
