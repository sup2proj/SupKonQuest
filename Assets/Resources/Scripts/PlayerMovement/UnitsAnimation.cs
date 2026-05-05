using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class UnitsAnimation : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 1f;
    public GameObject SelectionMarker;
    public NavMeshAgent agent;

    private bool isMovingToTarget = false;
    private SelectableObject selectable;
    private float stuckTimer = 0f;
    private static int priorityCounter = 0;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();

        agent.avoidancePriority = Mathf.Clamp(priorityCounter % 99, 1, 99);
        priorityCounter++;
    }

    void Start()
    {
        selectable = GetComponent<SelectableObject>();

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
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
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Récupère toutes les unités sélectionnées
            SelectableObject[] allSelectables = FindObjectsByType<SelectableObject>(FindObjectsSortMode.None);
            List<UnitsAnimation> selectedUnits = new List<UnitsAnimation>();

            foreach (var s in allSelectables)
                if (s.IsSelected)
                {
                    UnitsAnimation unit = s.GetComponent<UnitsAnimation>();
                    if (unit != null) selectedUnits.Add(unit);
                }

            // Seule la PREMIÈRE unité de la liste fait le dispatch
            if (selectedUnits.Count > 0 && selectedUnits[0] != this) return;

            for (int i = 0; i < selectedUnits.Count; i++)
            {
                Vector3 destination = GetFormationPosition(hit.point, i, selectedUnits.Count);
                selectedUnits[i].MoveTo(destination);
            }
        }
    }
}

    Vector3 GetFormationPosition(Vector3 center, int index, int total)
    {
        if (total == 1) return center;

        float radius = 0.3f * Mathf.Ceil(Mathf.Sqrt(total));
        float angle = index * (360f / total) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        Vector3 candidate = center + offset;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return center;
    }

    public void MoveTo(Vector3 destination)
    {
        agent.ResetPath();
        agent.SetDestination(destination);
        isMovingToTarget = true;
        stuckTimer = 0f;
    }

    void HandleMovement()
    {
        if (isMovingToTarget)
        {
            float dynamicStoppingDistance = Mathf.Max(agent.stoppingDistance, agent.speed * 0.1f);

            if (!agent.pathPending
                && agent.remainingDistance <= dynamicStoppingDistance
                && agent.velocity.sqrMagnitude < 0.01f)
            {
                isMovingToTarget = false;
                agent.ResetPath();
                stuckTimer = 0f;
            }
            else if (agent.velocity.magnitude < 0.05f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 1.5f)
                {
                    isMovingToTarget = false;
                    agent.ResetPath();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        bool isActuallyMoving = isMovingToTarget && agent.velocity.magnitude > 0.1f;
        if (animator != null)
            animator.SetBool("isMoving", isActuallyMoving);
    }
}