using UnityEngine;
using UnityEngine.InputSystem;

public class UnitInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] public UnitData unitData;

    [Header("Visuals")]
    public GameObject objectModel;
    public GameObject circleUnderFeet;
    public Animator animator;

    [Header("UI")]
    [SerializeField] public HealthBar healthBar;

    [Header("Runtime")]
    public float currentHealth;
    public int playerId;

    void Awake()
    {
        animator = GetComponent<Animator>();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

    }

    private void OnDestroy()
    {
        UnitsRegistry.Unregister(this);
    }

    void Start()
    {
        Initialize(unitData);
        UnitsRegistry.Register(this, playerId);

        if (objectModel != null)
            objectModel.SetActive(false);

        InitSelectionCircle();
        InitHealthBar();
    }

    void Update()
    {
        if (healthBar != null && healthBar.isActiveAndEnabled && Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            healthBar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        
        if (Input.GetKeyDown("b"))
        {
            if (unitData == null)
                return;
            const float damageAmount = 10f;
            TakeDamage(damageAmount);
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

        playerId = unitData.playerId;
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
            sr.color = GetSelectionColorForPlayer(playerId);
        }
    }

    private Color GetSelectionColorForPlayer(int id)
    {
        switch (id)
        {
            case 1: return Color.white;
            case 2: return Color.red;
            case 3: return Color.blue;
            case 4: return Color.green;
            case 5: return Color.yellow;
            case 6: return Color.cyan;
            case 7: return new Color(1f, 0.5f, 0f, 1f); // orange
            case 8: return new Color(0.6f, 0f, 1f, 1f); // violet
            default: return Color.gray;
        }
    }

    private void InitHealthBar()
    {
        if (healthBar == null)
        {
            Debug.LogWarning($"[UnitInstance] {name} : healthBar non assignée dans l'inspector.", this);
            return;
        }
        healthBar.transform.localPosition = (1.1f * Vector3.up);
    }

    public void TakeDamage(float amount)
    {
        if (unitData == null)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, unitData.maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        Destroy(circleUnderFeet, 0f);
        Destroy(healthBar, 0f);
        Destroy(gameObject, 0f);

        var pm = PlayerManager.Instance;
        var session = pm != null ? pm.GetSession(playerId) : null;
        if (session != null)
        {
            session.removeUnit(1);
            if (StatisticsInterface.Instance != null)
                StatisticsInterface.Instance.Refresh();
        }
    }

    public void ApplyBuff()
    {
        if (unitData == null)
            return;
        if (!(unitData is UnitSupportData))
            return;
        Debug.Log("Buff applied to " + unitData.type);
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
