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

    [Header("UI")]
    [SerializeField] public HealthBar healthBar;

    [Header("Runtime")]
    public float currentHealth;

    [Header("HealthBar Over Head")]
    [SerializeField] private Transform healthBarAnchor;
    [SerializeField] private float healthBarLocalY = 2f;
    [SerializeField] private Vector3 healthBarLocalOffset = Vector3.zero;
    [SerializeField] private bool healthBarFaceCamera = true;


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
        InitHealthBarOverHead();
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();

        // Maintenir le billboard sans avoir besoin d'un script séparé
        if (healthBarFaceCamera && healthBar != null && healthBar.isActiveAndEnabled && Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            healthBar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        if (Input.GetKeyDown("s"))
        {
            Heal(-10f);
        }
        if (Input.GetKeyDown("d"))
        {
            Heal(10f);
        }
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

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(unitData.maxHealth);
            healthBar.SetHealth(currentHealth);
        }
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

    private void InitHealthBarOverHead()
    {
        if (healthBar == null)
        {
            Debug.LogWarning($"[UnitInstance] {name} : healthBar non assignée dans l'inspector.", this);
            return;
        }

        // healthBar.gameObject.SetActive(true);

        // IMPORTANT: dans le prefab HealthBar, le RectTransform racine est à scale (0,0,0)
        // => donc invisible. On force un scale correct ici.
        if (healthBar.transform.localScale == Vector3.zero)
        {
            Debug.LogWarning($"[UnitInstance] {name} : HealthBar scale=0 détecté, correction en scale=1 (sinon invisible).", this);
            healthBar.transform.localScale = Vector3.one;
        }

        // On parent la healthbar sous un anchor (tête) ou sous l'unité, comme le circle.
        Transform anchor = healthBarAnchor != null ? healthBarAnchor : transform;
        if (healthBar.transform.parent != anchor)
            healthBar.transform.SetParent(anchor, worldPositionStays: false);

        // Position au-dessus de la tête
        healthBar.transform.localPosition = 1.1f * Vector3.up + healthBarLocalOffset;

        // Reset rotation locale pour éviter des rotations héritées cheloues
        healthBar.transform.localRotation = Quaternion.identity;

        // Optionnel: la faire regarder la caméra (sans script dédié)
        if (healthBarFaceCamera && Camera.main != null)
        {
            // On fait un premier snap tout de suite
            Vector3 forward = Camera.main.transform.forward;
            healthBar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
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
        if (unitData == null)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, unitData.maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        if (unitData == null)
            return;

        // Vérifier si c'est une unité soigneuse (logique actuelle conservée)
        if (!(unitData is UnitHealerData))
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, unitData.maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    void Die()
    {
        if (circleUnderFeet != null)
            circleUnderFeet.transform.SetParent(null);

        if (healthBar != null)
            healthBar.transform.SetParent(null);

        Destroy(gameObject, 2f);
    }

    public void ApplyBuff()
    {
        if (unitData == null)
            return;

        // Vérifier si c'est une unité de support
        if (!(unitData is UnitSupportData))
            return;

        Debug.Log("Buff applied to " + unitData.type);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignorer les collisions avec le sol (tiles)
        if (collision.gameObject.CompareTag("Ground"))
            return;

        // Vérifier si l'objet est une unité valide
        if (!collision.gameObject.CompareTag("Units"))
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

    public void SetHealth(float healthChange)
    {
        if (unitData == null)
            return;

        currentHealth += healthChange;
        currentHealth = Mathf.Clamp(currentHealth, 0f, unitData.maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }
}

