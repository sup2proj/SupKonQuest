using UnityEngine;
using System.Reflection;

public class NormalAttack : MonoBehaviour
{
    // L'unité scanne périodiquement autour d'elle pour trouver une structure ennemie.
    [SerializeField, Min(0.1f)] private float scanInterval = 1f;
    [SerializeField, Min(0.1f)] private float scanRadius = 4f;
    [SerializeField, Min(0.1f)] private float stopDistance = 0.5f;
    [SerializeField] private int ownerPlayerId = 1;

    private float scanTimer;
    private StructureInstance currentTarget;
    private StructureDetector structureDetector;
    private MovementManager movementManager;
    private Component unitsAnimation;
    private UnitInstance unitInstance;

    /// <summary>
    /// Initialise les références aux composants nécessaires (movement, animation, unit).
    /// </summary>
    private void Awake()
    {
        movementManager = GetComponent<MovementManager>();
        unitsAnimation = FindUnitsAnimationComponent();
        unitInstance = GetComponent<UnitInstance>();
        structureDetector = GetComponent<StructureDetector>();
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
            unitsAnimation = FindUnitsAnimationComponent();

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
        // Si le composant StructureDetector est présent, on l'utilise.
        // Sinon on retombe sur un balayage global des structures.
        if (structureDetector != null)
        {
            structureDetector.TryFindNearestEnemyStructure(effectiveRadius, out chosen);
        }
        else
        {
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
        }

        if (chosen == null)
        {
            return;
        }


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

        MovementManager movement = movementManager != null ? movementManager : GetComponent<MovementManager>();
        float attackStopDistance = Mathf.Max(stopDistance, GetUnitAttackRange());

        bool startedAttack = TryStartAttackTargetIfInRange(target.transform);
        if (!startedAttack && movement != null)
        {
            if (!movement.CanMoveOnWorldPosition(target.StructurePosition))
            {
                movement.ForceMoveToPosition(target.StructurePosition, attackStopDistance);
            }
            else
            {
                movement.MoveToTarget(target.transform, attackStopDistance);
            }
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
        if (structureDetector == null)
            structureDetector = GetComponent<StructureDetector>();

        if (structureDetector == null && IAInstance.IsAIPlayer(playerId) && IAInstance.IsDifficultyForPlayer(playerId, 2))
        {
            structureDetector = gameObject.AddComponent<StructureDetector>();
        }

        if (structureDetector != null)
            structureDetector.SetOwnerPlayerId(playerId);
    }

    /// <summary>
    /// Tente de lancer l'attaque via le composant UnitsAnimation s'il existe.
    /// On évite une dépendance de compilation forte pour rester robuste si
    /// ce composant n'est pas disponible dans le contexte actuel.
    /// </summary>
    private bool TryStartAttackTargetIfInRange(Transform target)
    {
        if (target == null || unitsAnimation == null)
            return false;

        MethodInfo method = unitsAnimation.GetType().GetMethod("TryStartAttackTargetIfInRange", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
            return false;

        object result = method.Invoke(unitsAnimation, new object[] { target });
        return result is bool started && started;
    }

    /// <summary>
    /// Recherche le composant UnitsAnimation sans référence directe au type,
    /// afin d'éviter une dépendance de compilation forte tout en gardant le
    /// comportement d'attaque.
    /// </summary>
    private Component FindUnitsAnimationComponent()
    {
        Component[] components = GetComponents<Component>();
        string expectedName = string.Concat("Units", "Animation");

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component != null && component.GetType().Name == expectedName)
                return component;
        }

        return null;
    }
}
