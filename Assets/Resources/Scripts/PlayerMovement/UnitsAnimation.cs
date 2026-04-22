using UnityEngine;
using UnityEngine.InputSystem;

public class UnitsAnimation : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 1f;

    private Vector3 targetPosition;
    private bool isMovingToTarget = false;

    private SelectableObject selectable;
    public GameObject SelectionMarker;
    public UnityEngine.AI.NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        Debug.Log("Awake called, agent assigned: " + (agent != null));
    }

    void Start()
    {
        selectable = GetComponent<SelectableObject>();

        // Synchronise la position de l'unité sur le NavMesh au démarrage
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    void Update()
    {
        HandleMouseClick();
        HandleMovement();
    }

    void HandleMouseClick()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && selectable.IsSelected)
        {
            Debug.Log("Clic droit détecté");
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Destination : " + hit.point);

                // FIX : déléguer le déplacement au NavMeshAgent, ne plus toucher transform.position
                agent.SetDestination(hit.point);
                isMovingToTarget = true;
            }
        }
    }

    void HandleMovement()
    {
        if (isMovingToTarget)
        {
            // L'agent a-t-il atteint sa destination ?
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isMovingToTarget = false;
            }
        }

        // FIX : utiliser la vélocité de l'agent pour piloter l'animation
        bool isActuallyMoving = isMovingToTarget && agent.velocity.magnitude > 0.1f;
        if (animator != null)
            animator.SetBool("isMoving", isActuallyMoving);

        // FIX : suppression du Quaternion.LookRotation manuel
        // Le NavMeshAgent gère la rotation via son angularSpeed (configurable dans l'Inspector)
    }
}