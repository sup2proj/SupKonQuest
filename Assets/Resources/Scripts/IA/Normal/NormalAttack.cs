using UnityEngine;

public class NormalAttack : MonoBehaviour
{
    // L'unité scanne périodiquement autour d'elle pour trouver une structure ennemie.
    [SerializeField, Min(0.1f)] private float scanInterval = 5f;
    [SerializeField, Min(0.1f)] private float scanRadius = 2f;
    [SerializeField, Min(0.1f)] private float stopDistance = 0.5f;
    [SerializeField] private int ownerPlayerId = 1;

    private float scanTimer;
    private StructureInstance currentTarget;

    private void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval)
            return;

        scanTimer = 0f;
        CheckForStructureInRadius();
    }

    private void CheckForStructureInRadius()
    {
        if (!StructureDetector.TryFindNearestEnemyStructureInRadius(
                transform.position,
                scanRadius,
                ownerPlayerId,
                out StructureInstance target))
        {
            return;
        }

        currentTarget = target;
        SendAttackOrder(currentTarget);
    }

    private void SendAttackOrder(StructureInstance target)
    {
        if (target == null)
            return;

        // Envoie juste un ordre de mouvement/attaque vers la structure.
        // Adapte ici à ton système réel (MovementManager, UnitController, etc.).
        MovementManager movement = GetComponent<MovementManager>();
        if (movement != null)
        {
            movement.MoveToPosition(target.StructurePosition, stopDistance);
            return;
        }

        // Fallback debug si aucun composant de mouvement.
        Debug.Log($"[NormalAttack] {name} cible {target.name} mais aucun MovementManager trouvé.");
    }

    // Optionnel: permet de setter le joueur depuis IAInstance/spawn.
    public void SetOwnerPlayerId(int playerId)
    {
        ownerPlayerId = playerId;
    }
}