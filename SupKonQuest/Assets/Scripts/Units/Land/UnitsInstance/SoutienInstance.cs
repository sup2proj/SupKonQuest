using UnityEngine;
using UnityEngine.InputSystem;

public class SoutienInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private Support supportData;
    
    [Header("Weapon")]
    [SerializeField] private GameObject batonModel; // Référence au modèle 3D de l'épée

    private float currentHealth;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (supportData != null)
        {
            currentHealth = supportData.maxHealth;
        }
        batonModel.SetActive(false);

    }
    void Update()
    {
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        Move(isMoving);
        
        bool isAttacking = Keyboard.current.wKey.isPressed;
        if (isAttacking)
        {
            batonModel.SetActive(isAttacking);
        }
        Attack(isAttacking );
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

    public void Init(Support data)
    {
        supportData = data;
        if (supportData != null)
        {
            currentHealth = supportData.maxHealth;
        }
    }
}