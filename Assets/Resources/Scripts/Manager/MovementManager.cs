using UnityEngine;
using UnityEngine.InputSystem;

public class MovementManager : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0.2f;
    public float rotationSpeed = 12f;

    [Header("Collision Avoidance")]
    [SerializeField] private LayerMask blockingLayers; // assigne "Units", "Obstacles", etc.
    [SerializeField] private float agentRadius = 0.35f;
    [SerializeField] private float agentHeight = 1.2f;
    [SerializeField] private float lookAheadDistance = 0.9f;
    [SerializeField] private float sideProbeAngle = 35f;
    [SerializeField] private float sideProbeDistance = 0.75f;

    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;

    private void Update()
    {
        HandleMouseClick();
        HandleMovement();
    }

    private void HandleMouseClick()
    {
        if (Mouse.current == null || Camera.main == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                targetPosition = hit.point;
                targetPosition.y = transform.position.y; // verrouille le plan horizontal
                isMovingToTarget = true;
            }
        }
    }

    private void HandleMovement()
    {
        if (!isMovingToTarget)
        {
            movement = Vector3.zero;
            return;
        }

        Vector3 current = transform.position;
        Vector3 toTarget = targetPosition - current;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance <= stoppingDistance)
        {
            isMovingToTarget = false;
            movement = Vector3.zero;
            return;
        }

        Vector3 desiredDir = toTarget.normalized;
        Vector3 resolvedDir = ResolveDirectionWithAvoidance(desiredDir);

        if (resolvedDir == Vector3.zero)
        {
            // Entièrement bloqué: stop pour éviter le tremblement.
            isMovingToTarget = false;
            movement = Vector3.zero;
            return;
        }

        movement = resolvedDir;
        transform.position += movement * moveSpeed * Time.deltaTime;

        Quaternion targetRot = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private Vector3 ResolveDirectionWithAvoidance(Vector3 desiredDir)
    {
        // 1) direction directe libre ?
        if (!IsBlocked(desiredDir, lookAheadDistance))
            return desiredDir;

        // 2) essaie gauche
        Vector3 leftDir = Quaternion.Euler(0f, -sideProbeAngle, 0f) * desiredDir;
        if (!IsBlocked(leftDir, sideProbeDistance))
            return leftDir.normalized;

        // 3) essaie droite
        Vector3 rightDir = Quaternion.Euler(0f, sideProbeAngle, 0f) * desiredDir;
        if (!IsBlocked(rightDir, sideProbeDistance))
            return rightDir.normalized;

        // 4) bloqué
        return Vector3.zero;
    }

    private bool IsBlocked(Vector3 dir, float distance)
    {
        Vector3 origin = transform.position + Vector3.up * (agentHeight * 0.5f);

        // Ignore les triggers pour éviter des faux positifs avec zones/UI 3D.
        bool hit = Physics.SphereCast(
            origin,
            agentRadius,
            dir,
            out RaycastHit hitInfo,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hit)
            return false;

        // Ignore son propre collider si besoin
        if (hitInfo.collider != null && hitInfo.collider.transform == transform)
            return false;

        return true;
    }

    // Optionnel: permet de lancer un déplacement depuis un autre script
    public void MoveTo(Vector3 destination)
    {
        targetPosition = destination;
        targetPosition.y = transform.position.y;
        isMovingToTarget = true;
    }
}
