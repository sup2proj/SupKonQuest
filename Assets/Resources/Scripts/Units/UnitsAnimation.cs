using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitsAnimation : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 1f;

    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private float stoppingDistance = 0.1f;
    private Transform followTarget;

    // Etat d'attaque continue
    private bool autoAttackLoop = false;
    private Transform attackTarget;
    private float attackRange = 0f;

    private SelectableObject selectable;
    public GameObject SelectionMarker;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        selectable = GetComponent<SelectableObject>();
    }

    void Update()
    {
        HandleMouseClick();
        HandleMovement();
        HandleAutoAttackLoop();

        if (animator != null)
        {
            animator.SetBool("isMoving", isMovingToTarget);
            UnitInstance unit = GetComponent<UnitInstance>();
            if (unit != null && unit.objectModel != null && unit.unitData != null)
            {
                // Archer visible pendant le déplacement
                if (unit.unitData.type == UnitsType.Archer && isMovingToTarget)
                {
                    unit.objectModel.SetActive(true);
                }
            }
            
        }
    }

    void HandleMouseClick()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && selectable.IsSelected)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                MoveToPosition(hit.point, 0.1f);
            }
        }
    }

    void HandleMovement()
    {
        if (!isMovingToTarget)
        {
            movement = Vector3.zero;
            return;
        }

        if (followTarget != null)
            targetPosition = followTarget.position;

        // Distance en XZ pour éviter les soucis de hauteur
        Vector3 flatCurrent = transform.position;
        Vector3 flatTarget = targetPosition;
        flatCurrent.y = 0f;
        flatTarget.y = 0f;

        float distance = Vector3.Distance(flatCurrent, flatTarget);

        if (distance <= stoppingDistance)
        {
            isMovingToTarget = false;
            movement = Vector3.zero;

            if (animator != null)
                animator.SetBool("isMoving", false);

            // Si on suivait une unité ennemie: démarre la boucle d'attaque
            if (followTarget != null)
            {
                attackTarget = followTarget;
                autoAttackLoop = true;

                if (animator != null)
                    animator.SetBool("isAttacking", true);

                UnitInstance unit = GetComponent<UnitInstance>();
                if (unit != null && unit.objectModel != null)
                    unit.objectModel.SetActive(true);
            }

            followTarget = null;
            return;
        }

        Vector3 direction = (flatTarget - flatCurrent).normalized;
        movement = new Vector3(direction.x, 0f, direction.z);

        transform.position += movement * moveSpeed * Time.deltaTime;

        if (movement.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    void HandleAutoAttackLoop()
{
    UnitInstance unit = GetComponent<UnitInstance>();

    if (!autoAttackLoop)
    {
        if (animator != null)
            animator.SetBool("isAttacking", false);

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(false);

        return;
    }

    if (attackTarget == null)
    {
        autoAttackLoop = false;

        if (animator != null)
            animator.SetBool("isAttacking", false);

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(false);

        return;
    }

    UnitInstance targetUnit = attackTarget.GetComponent<UnitInstance>();

    // Stoppe la boucle si la cible n'est plus valide ou déjà morte
    if (targetUnit == null || targetUnit.currentHealth <= 0f)
    {
        autoAttackLoop = false;
        attackTarget = null;

        if (animator != null)
            animator.SetBool("isAttacking", false);

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(false);

        return;
    }

    Vector3 a = transform.position; a.y = 0f;
    Vector3 b = attackTarget.position; b.y = 0f;
    float dist = Vector3.Distance(a, b);

    // Tant que la cible est en portée et vivante, l'attaque reste active en boucle.
    if (dist <= Mathf.Max(0.01f, attackRange))
    {
        if (animator != null)
            animator.SetBool("isAttacking", true);

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(true);

        Vector3 lookDir = (b - a).normalized;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(lookDir.x, 0f, lookDir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
    else
    {
        // Hors portée: on arrête l'attaque (tu pourras ajouter la poursuite plus tard si besoin).
        autoAttackLoop = false;

        if (animator != null)
            animator.SetBool("isAttacking", false);

        if (unit != null && unit.objectModel != null)
            unit.objectModel.SetActive(false);
    }
}



    public void MoveToPosition(Vector3 destination, float stopDistance = 0.1f)
    {
        followTarget = null;
        attackTarget = null;
        autoAttackLoop = false;

        targetPosition = destination;
        targetPosition.y = transform.position.y;
        stoppingDistance = Mathf.Max(0.01f, stopDistance);
        isMovingToTarget = true;
    }

    public void MoveToTarget(Transform target, float stopDistance)
    {
        if (target == null)
            return;

        followTarget = target;
        attackTarget = null;
        autoAttackLoop = false;

        targetPosition = target.position;
        targetPosition.y = transform.position.y;
        attackRange = Mathf.Max(0.01f, stopDistance);
        stoppingDistance = attackRange; // arrêt à la portée d'attaque
        isMovingToTarget = true;
    }
}
