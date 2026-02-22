using UnityEngine;
using UnityEngine.InputSystem;

public class UnitsHealerInstance : MonoBehaviour
{
    [Header("Data")] 
    [SerializeField] private HealerData healerData;
    public GameObject itemModel;
    public float currentHealth;
    public float maxHealth;
    public Animator animator;
    public int price;
    public float buffHealing;
    public float buffHealingCooldown;
    public float buffTime;
    public float buffRange;
    public float speed;
    public float creationTime;
    public UnitsType type;
    public bool isPoweredUnit = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    // Méthode d'initialisation pour configurer les données de l'unité healer à partir du scriptable object
    
    void Initialize(HealerData data)
    {
        healerData = data;
        if (healerData != null)
        {
            currentHealth = healerData.maxHealth;
            maxHealth = healerData.maxHealth;
            price = healerData.price;
            buffHealing = healerData.buffHealing;
            buffHealingCooldown = healerData.buffHealingCooldown;
            buffTime = healerData.buffTime;
            buffRange = healerData.buffRange;
            speed = healerData.speed;
            creationTime = healerData.creationTime;
            type = healerData.type;
            isPoweredUnit = healerData.isPoweredUnit;
        }
    }

    void Start()
    {
        Initialize(healerData);
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

    public void Init(HealerData data)
    {
        healerData = data;
        if (healerData != null)
        {
            currentHealth = healerData.maxHealth;
            maxHealth = healerData.maxHealth;
        }
    }
}
