using UnityEngine;

public static class StructureDetector
{
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

    /// <summary>
    /// Tentative de recherche d'une structure ennemie proche. Si trouvée,
    /// la cible est écrite dans <paramref name="target"/> et la méthode
    /// retourne true.
    /// </summary>
    /// <param name="origin">Position depuis laquelle la recherche est effectuée.</param>
    /// <param name="radius">Rayon de recherche.</param>
    /// <param name="ownerPlayerId">Identifiant du joueur à ignorer (propre joueur).</param>
    /// <param name="target">Sortie contenant la structure trouvée ou null.</param>
    /// <returns>True si une structure ennemie a été trouvée.</returns>
    public static bool TryFindNearestEnemyStructureInRadius(
        Vector3 origin,
        float radius,
        int ownerPlayerId,
        out StructureInstance target)
    {
        target = FindNearestEnemyStructureInRadius(origin, radius, ownerPlayerId);
        return target != null;
    }
}