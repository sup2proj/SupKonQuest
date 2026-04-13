using UnityEngine;
using UnityEngine.InputSystem;

public class MovementManager : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0.2f;
    public float rotationSpeed = 12f;

    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;

    private UnitInstance unitInstance;

    private void Awake()
    {
        unitInstance = GetComponent<UnitInstance>();
    }

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
                targetPosition.y = transform.position.y;
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
        Vector3 resolvedDir =  Vector3.zero;

        if (resolvedDir == Vector3.zero)
        {
            isMovingToTarget = false;
            movement = Vector3.zero;
            return;
        }

        movement = resolvedDir;

        float finalSpeed = moveSpeed;
        GetUnitSpeed(finalSpeed);
        transform.position += movement * finalSpeed * Time.deltaTime;

        Quaternion targetRot = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private public GetUnitSpeed(float finalSpeed)
    {
        if (unitInstance != null && unitInstance.unitData != null)
        {
            finalSpeed = unitInstance.unitData.speed;
        }
        return finalSpeed;
    }
}
