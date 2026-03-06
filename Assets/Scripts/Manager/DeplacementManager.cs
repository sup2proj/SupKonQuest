using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;


public class DeplacementManager : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 1f;
    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private float stoppingDistance = 0.1f;
    private InputAction moveAction;

    /*
    void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetPosition = transform.position;
    }
    */

    // Update is called once per frame
    void Update()
    {
        HandleMouseClick();
        HandleMovement();
        /*

        if(isMovingToTarget)
        {
            animator.SetBool("Courir", true);
        }
        else
        {
            animator.SetBool("Courir", false);
        }
        */
    }

    void HandleMouseClick()
    {
        // Détector le clic gauche de la souris
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            // Créer un raycast pour trouver où on a cliqué
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                targetPosition = hit.point;
                isMovingToTarget = true;
            }
        }
    }

    void HandleMovement()
    {
        if (isMovingToTarget)
        {
            // Se déplacer vers la cible
            movement = (targetPosition - transform.position).normalized;
            
            // Vérifier si on est arrivé
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance < stoppingDistance)
            {
                isMovingToTarget = false;
                movement = Vector3.zero;
            }
        }
        else
        {
            movement = Vector3.zero;
        }

        // Déplacer le personnage
        transform.position += movement * moveSpeed * Time.deltaTime;

        // Tourner le personnage dans la direction du mouvement
        if (movement.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}

