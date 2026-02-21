using UnityEngine;
using UnityEngine.InputSystem;

public class ArcherInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsData unitData;
    
    [Header("Weapon")]
    [SerializeField] private GameObject bowModel;

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
        bowModel.SetActive(false);

    }
    void Update()
    {
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        if (isMoving)
        {
            bowModel.SetActive(isMoving);
        }
        Move(isMoving);
        
        bool isAttacking = Keyboard.current.wKey.isPressed;
        if (isAttacking)
        {
            bowModel.SetActive(isAttacking);
        }
        Attack(isAttacking);
    }

    public void Move(bool isMoving)
    {
        animator.SetBool("isMoving", isMoving);
    }
    
    public void Attack(bool isAttacking)
    {
        animator.SetBool("isAttacking", isAttacking);
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