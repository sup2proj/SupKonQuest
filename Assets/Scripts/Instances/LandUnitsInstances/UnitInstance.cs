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

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
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
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 move = transform.forward * unitData.speed * Time.deltaTime;
                rb.MovePosition(rb.position + move);
            }
            else
            {
                transform.Translate(Vector3.forward * unitData.speed * Time.deltaTime);
            }
        }
    }

    void HandleAttack()
    {
        bool isAttacking = Keyboard.current.gKey.isPressed;

        if (animator != null)
        {
            animator.SetBool("isAttacking", isAttacking);
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
    
    private void OnCollisionEnter(Collision collision)
    {
        // Ignorer les collisions avec le sol (tiles)
        if (collision.gameObject.CompareTag("Ground"))
            return;

        // Vérifier si l'objet est une unité valide
        if (!collision.gameObject.CompareTag("AntiBlindage") &&
            !collision.gameObject.CompareTag("Archer"))
            return;

        Debug.Log("Collision détectée avec : " + collision.gameObject.name);

        // Récupérer le Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();

        // Calculer la direction pour repousser l'unité
        Vector3 pushDirection = transform.position - collision.contacts[0].point;
        pushDirection.y = 0f;
        pushDirection.Normalize();

        // Appliquer une force de recul
        float pushForce = 5f;
        rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
    }
}