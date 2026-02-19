using UnityEngine;
using UnityEngine.InputSystem;

public class UnitInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsData unitData;
    
    [Header("Weapon")]
    [SerializeField] private GameObject swordModel; // Référence au modèle 3D de l'épée

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
        swordModel.SetActive(false);

    }
    void Update()
    {
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        Move(isMoving);
        
        bool isAttacking = Keyboard.current.wKey.isPressed;
        if (isAttacking)
        {
            swordModel.SetActive(isAttacking);
        }
        Attack(isAttacking );
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
