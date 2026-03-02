using UnityEngine;
using UnityEngine.InputSystem;

public class UnitsSoldierInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsData unitData;
    
    public GameObject weaponModel;
    public GameObject circleUnderFeet;
    public float currentHealth;
    public float maxHealth;
    public Animator animator;
    public int price;
    public float attackRange;
    public float speed;
    public float creationTime;
    public UnitsType type;
    public bool isPoweredUnit;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    // Méthode d'initialisation pour configurer les données de l'unité, elle récupère les données à partir du scriptable object
    // et les assigne aux variables de l'instance.
    
    void initialize(UnitsData data)
    {
        unitData = data;
        if (unitData != null)
        {
            currentHealth = unitData.maxHealth;
            maxHealth = unitData.maxHealth;
            price = unitData.price;
            maxHealth = unitData.maxHealth;
            attackRange = unitData.attackRange;
            speed = unitData.speed;
            creationTime = unitData.creationTime;
            type = unitData.type;
            isPoweredUnit = unitData.isPoweredUnit;
        }
    }

    void Start()
    {
        initialize(unitData);
        if (weaponModel != null)
        {
            weaponModel.SetActive(false);
        }
        
        
        if (circleUnderFeet != null)
        {
            circleUnderFeet.SetActive(true);

            // Ajuste la position
            Vector3 localPos = circleUnderFeet.transform.localPosition;
            localPos.y = 0f;
            circleUnderFeet.transform.localPosition = localPos;

            SpriteRenderer sr = circleUnderFeet.GetComponent<SpriteRenderer>();
            // à modifier en fonction de la couleur attribué au joueur
            if (sr != null)
            {
                sr.color = Color.white;
            }
        }
    }

    void Update()
    {
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        Move(isMoving);
        
        bool isAttacking = Keyboard.current.wKey.isPressed;
        if (isAttacking && weaponModel != null)
        {
            weaponModel.SetActive(isAttacking);
        }
        Attack(isAttacking );
    }

    public void takeDamage(int amount){
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void Heal(int amount){
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    
    public void Die()
    {
        // Détacher le cercle avant destruction pour qu'il ne disparaisse pas
        if (circleUnderFeet != null)
        {
            circleUnderFeet.transform.SetParent(null);
        }
        
        // Jouer l'animation de mort
        animator.SetTrigger("Die");
        // Supprime l'objet après un délai pour permettre à l'animation de se jouer
        GameObject.Destroy(gameObject, 2f); 
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
