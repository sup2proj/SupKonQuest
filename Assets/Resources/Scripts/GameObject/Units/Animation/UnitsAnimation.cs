using UnityEngine;
using System.Collections;

public partial class UnitsAnimation : MonoBehaviour
{
    public Animator animator;

    private Transform attackTarget;
    private UnitInstance cachedUnit;
    private Coroutine attackCoroutine;
    private bool isRetaliating;
    private DamageTable damageTable;

    private MovementManager movementManager;

    public Transform AttackTarget => attackTarget;
    public GameObject cannonBallPrefab;
    public float cannonBallSpeed = 3f;
 	private float oldSpeedAnimation;

    void Awake()
    {
        if (animator == null)
			{
            	animator = GetComponent<Animator>();
				 oldSpeedAnimation = animator.speed;
			}
        cachedUnit = GetComponent<UnitInstance>();
        movementManager = GetComponent<MovementManager>();

        damageTable = UnityEngine.Resources.Load<DamageTable>("Scripts/Data/Units/UnitsSO/DamageTable");
        if (damageTable == null)
            Debug.LogError("Impossible de charger DamageTable !");

        bool isProjectileUnit = cachedUnit != null && cachedUnit.unitData != null && (
        cachedUnit.unitData.type == UnitsType.Fregate
        || cachedUnit.unitData.type == UnitsType.Destroyer);

        if (cachedUnit != null && cachedUnit.unitData != null && cachedUnit.unitData.type == UnitsType.Mortar) {
            cannonBallPrefab = Resources.Load<GameObject>("Prefabs/Units/CannonBall/Cannonball");
            if (cannonBallPrefab == null)
                Debug.LogError("[Mortar] CannonBall prefab introuvable dans Resources/Prefabs/");
        }
        
        if (isProjectileUnit)
        {
            cannonBallPrefab = Resources.Load<GameObject>("Prefabs/Units/CannonBall/Cannonball");
            if (cannonBallPrefab == null)
                Debug.LogError("[Projectile] CannonBall prefab introuvable !");
        }
    }

    void Start()
    {
        if (movementManager != null)
        {
            movementManager.OnMovementComplete += OnMovementCompleted;
            // Les méthodes d'attaque, de mouvement et de gestion des spells ont été déplacées
            // dans des fichiers partiels séparés : Attack.cs, Movement.cs et Spell.cs.
            // Elles sont toujours accessibles via cette classe partielle `UnitsAnimation`.
        }
    }

    void Update()
    {
        // Les méthodes appelées sont définies dans les fichiers partiels
        HandleAutoAttack();
        RefreshMovingArcherModel();
    }

    /// <summary>
    /// Retourne true si l'unité peut attaquer avec des dégâts (exclut Support/Healer).
    /// </summary>
    private bool CanAttackWithDamage(UnitInstance unit)
    {
        if (unit == null || unit.unitData == null)
            return false;

        return unit.unitData.type != UnitsType.Support && unit.unitData.type != UnitsType.Healer;
    }

    /// <summary>
    /// Active/désactive les visuels d'attaque (animation + modèle d'objet).
    /// </summary>
    private void SetAttackVisuals(bool visible)
    {
        // SetAttackAnimationState est défini dans la partie Attack.cs
        SetAttackAnimationState(visible);
        SetObjectModelVisible(visible);
    }

    /// <summary>
    /// Active/désactive l'objet visuel de l'unité.
    /// </summary>
    private void SetObjectModelVisible(bool visible)
    {
        UnitInstance unit = cachedUnit;
        if (unit == null)
            unit = GetComponent<UnitInstance>();

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(visible);
    }

    /// <summary>
    /// Démarre la boucle d'attaque appropriée (bateau ou normale) et prépare les visuels.
    /// </summary>
    private void BeginAttackLoop()
    {
        SetAttackVisuals(true);
        StopAttackCoroutine();

        bool isProjectileUnit = cachedUnit != null && cachedUnit.unitData != null && (
            cachedUnit.unitData.type == UnitsType.Fregate || cachedUnit.unitData.type == UnitsType.Destroyer);

        attackCoroutine = isProjectileUnit ? StartCoroutine(BoatAttackLoopCoroutine()) : StartCoroutine(AttackLoopCoroutine());
    }

    /// <summary>
    /// Arrête la coroutine d'attaque en cours (si présente) et remet la vitesse d'animation.
    /// </summary>
    private void StopAttackCoroutine()
    {
        if (attackCoroutine == null)
            return;

        StopCoroutine(attackCoroutine);
        attackCoroutine = null;

        if (animator != null)
            animator.speed = oldSpeedAnimation;
    }

    /// <summary>
    /// Tente de récupérer l'unité/structure cible actuelle et vérifie qu'elle est en vie.
    /// </summary>
    private bool TryGetCurrentTarget(out UnitInstance targetUnit, out StructureInstance targetStructure)
    {
        targetUnit = GetTargetUnit();
        targetStructure = targetUnit == null ? GetTargetStructure() : null;

        if (targetUnit == null && targetStructure == null)
            return false;

        if (targetUnit != null && targetUnit.currentHealth <= 0)
            return false;

        if (targetStructure != null && targetStructure.currentHealth <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Retourne true si la structure cible appartient au même joueur que l'attaquant.
    /// </summary>
    private bool IsFriendlyStructureTarget(StructureInstance targetStructure, UnitInstance attackerUnit)
    {
        return targetStructure != null && attackerUnit != null && targetStructure.playerId == attackerUnit.playerId;
    }
}