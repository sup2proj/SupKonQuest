using UnityEngine;
using System.Collections;

public class StructureAttack : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject cannonBallPrefab;
    public float cannonBallSpeed = 5f;
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float attackCooldown = 3f;

    private StructureInstance structureInstance;
    private float lastAttackTime = -999f;

    /// <summary>
    /// Récupère la structure associée et charge le projectile utilisé pour les attaques.
    /// </summary>
    void Awake()
    {
        structureInstance = GetComponent<StructureInstance>();

        cannonBallPrefab = Resources.Load<GameObject>("Prefabs/Units/CannonBall/Cannonball");
        if (cannonBallPrefab == null)
            Debug.LogError("[StructureAttack] CannonBall prefab introuvable !");
    }

    /// <summary>
    /// Cherche des unités ennemies à portée pour déclencher une attaque automatique.
    /// </summary>
    void Update()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        UnitInstance[] allUnits = FindObjectsByType<UnitInstance>(FindObjectsSortMode.None);
        foreach (UnitInstance unit in allUnits)
        {
            if (unit == null || unit.currentHealth <= 0)
                continue;

            if (unit.playerId == structureInstance.playerId)
                continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist <= attackRange)
            {
                lastAttackTime = Time.time;
                ShootAt(unit);
                break;
            }
        }
    }
    /// <summary>
    /// Réagit lorsqu'une unité attaque la structure en tentant une riposte.
    /// </summary>
    public void OnAttacked(UnitInstance attacker)
    {
        if (attacker == null || attacker.transform == null)
            return;

        if (Time.time - lastAttackTime < attackCooldown)
            return;

        float dist = Vector3.Distance(transform.position, attacker.transform.position);
        if (dist > attackRange)
            return;

        lastAttackTime = Time.time;
        ShootAt(attacker);
    }

    /// <summary>
    /// Tire un projectile sur une unité cible et applique les dégâts à l'arrivée.
    /// </summary>
    private void ShootAt(UnitInstance target)
    {
        if (cannonBallPrefab == null || target == null)
            return;

        Vector3 spawnPos = transform.position + Vector3.up * 1f;
        UnitInstance attackerSnapshot = target;
        StructureInstance structureSnapshot = structureInstance;

        CannonBall.Spawn(cannonBallPrefab, spawnPos, target.transform, cannonBallSpeed, (impactPos) =>
        {
            if (attackerSnapshot == null || attackerSnapshot.currentHealth <= 0)
                return;

            attackerSnapshot.TakeDamage(attackDamage, null, structureSnapshot.playerId);

            UnitsAnimation anim = attackerSnapshot.GetComponent<UnitsAnimation>();
            if (anim != null)
                anim.AttackTheAttacker(structureSnapshot.transform);

        }, transform, arcHeight: 1.5f, scale: 0.2f);
    }
}