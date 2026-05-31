using UnityEngine;

public class StructureDetector : MonoBehaviour
{
    [Header("Detector")]
    [SerializeField] private int ownerPlayerId = 1;

    /// <summary>
    /// Définit l'identifiant du joueur propriétaire utilisé pour ignorer
    /// les structures amies lors de la détection.
    /// </summary>
    public void SetOwnerPlayerId(int id)
    {
        ownerPlayerId = id;
    }

    /// <summary>
    /// Tente de trouver la structure ennemie la plus proche autour de ce
    /// composant en utilisant la logique interne (enveloppe autour des
    /// méthodes statiques existantes).
    /// </summary>
    public bool TryFindNearestEnemyStructure(float radius, out StructureInstance target)
    {
        target = FindNearestEnemyStructureInRadius(transform.position, radius, ownerPlayerId);
        return target != null;
    }

    /// <summary>
    /// Recherche la structure ennemie la plus proche de <paramref name="origin"/>
    /// à l'intérieur du rayon <paramref name="radius"/> et excluant les
    /// structures appartenant à <paramref name="ownerPlayerId"/>.
    /// Retourne la <see cref="StructureInstance"/> trouvée ou null si aucune.
    /// </summary>
    /// <param name="origin">Position depuis laquelle la recherche est effectuée.</param>
    /// <param name="radius">Rayon de recherche.</param>
    /// <param name="ownerPlayerId">Identifiant du joueur à ignorer (propre joueur).</param>
    public static StructureInstance FindNearestEnemyStructureInRadius(Vector3 origin, float radius, int ownerPlayerId)
    {
        StructureInstance[] structures = Object.FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
        if (structures == null || structures.Length == 0)
            return null;

        float radiusSqr = radius * radius;
        float bestDistSqr = float.MaxValue;
        StructureInstance best = null;

        for (int i = 0; i < structures.Length; i++)
        {
            StructureInstance s = structures[i];
            if (s == null)
                continue;

            // Ignore les structures du même joueur.
            if (s.playerId == ownerPlayerId)
                continue;

            Vector3 targetPos = s.StructurePosition;
            float distSqr = (targetPos - origin).sqrMagnitude;

            if (distSqr > radiusSqr)
                continue;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                best = s;
            }
        }

        return best;
    }
}