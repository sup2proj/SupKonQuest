using UnityEngine;

public static class StructureDetector
{
    // Retourne la structure ennemie la plus proche dans le rayon (null si rien trouvé).
    public static StructureInstance FindNearestEnemyStructureInRadius(Vector3 origin, float radius, int ownerPlayerId)
    {
        StructureInstance[] structures = Object.FindObjectsOfType<StructureInstance>();
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