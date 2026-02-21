using UnityEngine;
using UnityEngine.InputSystem;

public class InfanterieInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsData unitData;
    
    [Header("Weapon")]
    [SerializeField] private GameObject swordModel; // Référence au modèle 3D de l'épée

    private float currentHealth;
    private Animator animator;
    public int price;
    public float maxHealth;
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
