using UnityEngine;
using UnityEngine.InputSystem;

public class HeavyInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsData unitData;
    
    [Header("Weapon")]
    [SerializeField] private GameObject swordModel; 

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
        if (swordModel != null)
        {
            swordModel.SetActive(false);
        }
    }
    
    void Update()
    {
        if (Keyboard.current == null) return;
        
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        Move(isMoving);
        
        bool isAttacking = Keyboard.current.wKey.isPressed;
        if (isAttacking && swordModel != null)
        {
            swordModel.SetActive(isAttacking);
        }
        Attack(isAttacking);
    }

    public void Move(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
        }
    }
    
    public void Attack(bool isAttacking)
    {
        if (animator != null)
        {
            animator.SetBool("isAttacking", isAttacking);
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