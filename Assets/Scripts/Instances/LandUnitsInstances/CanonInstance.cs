using UnityEngine;
using UnityEngine.InputSystem;

public class CanonInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsData unitData;
    

    private float currentHealth;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (unitData != null)
        {
            currentHealth = unitData.maxHealth;
        }
    }
    
    void Update()
    {
        if (Keyboard.current == null) return;
        
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        Move(isMoving);
    }

    public void Move(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
        }
    }
    
    public void Init(UnitsData data)
    {
        unitData = data;
        if (unitData != null)
        {
            currentHealth = unitData.maxHealth;
        }
    }
}