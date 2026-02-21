using UnityEngine;
using UnityEngine.InputSystem;

public class UnitsBufferInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private Support buffData;
    public GameObject itemModel;
    public float currentHealth;
    public float maxHealth;
    public Animator animator;
    public int price;
    public float buffAttackSpeed;
    public float buffSpeed;
    public float buffDamage;
    public float buffRange;
    public float speed;
    public float creationTime;
    public UnitsType type;
    public bool isPoweredUnit;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    // Méthode d'initialisation pour configurer les données de l'unité buffer à partir du scriptable object
    
    void Initialize(Support data)
    {
        buffData = data;
        if (buffData != null)
        {
            currentHealth = buffData.maxHealth;
            maxHealth = buffData.maxHealth;
            price = buffData.price;
            buffAttackSpeed = buffData.buffAttackSpeed;
            buffSpeed = buffData.buffSpeed;
            buffDamage = buffData.buffDamage;
            buffRange = buffData.buffRange;
            speed = buffData.speed;
            creationTime = buffData.creationTime;
            type = buffData.type;
            isPoweredUnit = buffData.isPoweredUnit;
        }
    }

    void Start()
    {
        Initialize(buffData);
        itemModel.SetActive(false);
    }

    void Update()
    {
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        Move(isMoving);
        
        bool isActivatingBuff = Keyboard.current.wKey.isPressed;
        if (isActivatingBuff)
        {
            itemModel.SetActive(isActivatingBuff);
        }
        Attack(isActivatingBuff);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    
    public void Die()
    {
        // Jouer l'animation de mort
        animator.SetTrigger("Die");
        // Supprime l'objet après un délai pour permettre à l'animation de se jouer
        GameObject.Destroy(gameObject, 2f); 
    }
    
    public void Move(bool isMoving)
    {
        animator.SetBool("isMoving", isMoving);
    }
    
    public void Attack(bool isActivatingBuff)
    {
        animator.SetBool("isAttacking", isActivatingBuff);
    }

    public void Init(Support data)
    {
        buffData = data;
        if (buffData != null)
        {
            currentHealth = buffData.maxHealth;
        }
    }
}
