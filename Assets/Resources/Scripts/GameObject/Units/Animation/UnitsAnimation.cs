using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public partial class UnitsAnimation : MonoBehaviour
{
    public Animator animator;

    private Transform attackTarget;
    private int activeGroupMoveId = -1;
    private bool isGroupLeader = false;

    private static readonly HashSet<int> completedGroupMoves = new HashSet<int>();

    private UnitInstance cachedUnit;
    private Coroutine attackCoroutine;
    private Coroutine spellAttackResetCoroutine;
    private Coroutine ensureAttackAnimationCoroutine;
    private bool isRetaliating;
    private DamageTable damageTable;

    private MovementManager movementManager;

    public Transform AttackTarget => attackTarget;
    public GameObject cannonBallPrefab;
    public float cannonBallSpeed = 3f;

    /// <summary>
    /// Initialise les références nécessaires à l'animation, au combat et au mouvement de l'unité.
    /// </summary>
    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        cachedUnit = GetComponent<UnitInstance>();
        movementManager = GetComponent<MovementManager>();

        damageTable = Resources.Load<DamageTable>("Scripts/Data/Units/UnitsSO/DamageTable");
        if (damageTable == null)
        {
            Debug.LogError("Impossible de charger DamageTable !");
        }
        if (cachedUnit != null && cachedUnit.unitData != null && cachedUnit.unitData.type == UnitsType.Mortar) {
            cannonBallPrefab = Resources.Load<GameObject>("Prefabs/Units/CannonBall/Cannonball");
            if (cannonBallPrefab == null)
                Debug.LogError("[Mortar] CannonBall prefab introuvable dans Resources/Prefabs/");
        }

        if (movementManager != null)
        {
            movementManager.OnMovementComplete += OnMovementCompleted;
        }
    }

    /// <summary>
    /// Met à jour les comportements d'attaque automatique et les ajustements visuels liés au mouvement.
    /// </summary>
    void Update()
    {
        HandleAutoAttack();
        if (animator != null && movementManager != null)
        {
            UnitInstance unit = cachedUnit;
            if (unit != null && unit.objectModel != null && unit.unitData != null)
            {
                if (unit.unitData.type == UnitsType.Archer && movementManager.IsMoving())
                {
                    unit.objectModel.SetActive(true);
                }
            }
        }
    }
}