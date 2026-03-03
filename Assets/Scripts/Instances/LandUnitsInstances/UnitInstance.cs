using UnityEngine;
using UnityEngine.InputSystem;

public class UnitInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitData unitData;

    [Header("Visuals")]
    public GameObject objectModel;
    public GameObject circleUnderFeet;
    public Animator animator;

    [Header("Runtime")]
    public float currentHealth;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        Initialize(unitData);

        if (objectModel != null)
            objectModel.SetActive(false);

        InitSelectionCircle();
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();
    }

    public void Initialize(UnitData data)
    {
        unitData = data;
        if (unitData == null)
        {
            Debug.LogError("UnitData is null on " + gameObject.name);
            return;
        }
        currentHealth = unitData.maxHealth;
    }

    void InitSelectionCircle()
    {
        if (circleUnderFeet == null)
            return;

        circleUnderFeet.SetActive(true);

        Vector3 localPos = circleUnderFeet.transform.localPosition;
        localPos.y = 0f;
        circleUnderFeet.transform.localPosition = localPos;

        SpriteRenderer sr = circleUnderFeet.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
        }
    }

    void HandleMovement()
    {
        if (unitData == null)
            return;

        bool isMoving = Keyboard.current.spaceKey.isPressed;
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            transform.Translate(Vector3.forward * unitData.speed * Time.deltaTime);
        }
    }

    void HandleAttack()
    {
        // Vérifier si c'est une unité de combat
        if (!(unitData is UnitCombatData))
            return;

        bool isAttacking = Keyboard.current.gKey.isPressed;

        if (animator != null)
        {
            animator.SetBool("isAttacking", isAttacking);
            Debug.Log("Animation isAttacking définie à: " + isAttacking + " pour " + gameObject.name);
        }
        else
        {
            Debug.LogError("Animator est null dans HandleAttack pour " + gameObject.name);
        }
        
        if (objectModel != null)
        {
            objectModel.SetActive(isAttacking);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        if (unitData == null)
            return;

        // Vérifier si c'est une unité soigneuse
        if (!(unitData is UnitHealerData healerData))
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, unitData.maxHealth);
    }

    void Die()
    {
        if (circleUnderFeet != null)
            circleUnderFeet.transform.SetParent(null);
        Destroy(gameObject, 2f);
    }

    public void ApplyBuff()
    {
        if (unitData == null)
            return;

        // Vérifier si c'est une unité de support
        if (!(unitData is UnitSupportData supportData))
            return;

        Debug.Log("Buff applied to " + unitData.type);
    }
}