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
    public bool isNeutral;
    private StructureInstance protectorSourceStructure;
    private int lastAttackerPlayerId = -1;

    /// <summary>
    /// Initialise les références physiques et visuelles de l'unité au chargement.
    /// </summary>
    void Awake()
    {
        animator = GetComponent<Animator>();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

    }

    /// <summary>
    /// Retire l'unité du registre global lorsqu'elle est détruite.
    /// </summary>
    private void OnDestroy()
    {
        UnitsRegistry.Unregister(this);
    }

    /// <summary>
    /// Initialise les données runtime, les registres et les éléments visuels de base.
    /// </summary>
    void Start()
    {
        Initialize(unitData);
        UnitsRegistry.Register(this, playerId);

        if (objectModel != null)
            objectModel.SetActive(false);

        InitSelectionCircle();
        InitHealthBar();
    }

    /// <summary>
    /// Met à jour l'orientation de la barre de vie et gère le test de dégâts temporaire.
    /// </summary>
    void Update()
    {
        if (healthBar != null && healthBar.isActiveAndEnabled && Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            healthBar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    /// <summary>
    /// Charge les données de l'unité et remet ses valeurs runtime à l'état initial.
    /// </summary>
    public void Initialize(UnitData data)
    {
        unitData = data;
        if (unitData == null)
        {
            return;
        }

        playerId = unitData.playerId;
        isNeutral = unitData.isNeutral;
        currentHealth = unitData.maxHealth;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(unitData.maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }

    /// <summary>
    /// Active ou désactive l'état neutre de l'unité.
    /// </summary>
    public void SetNeutralState(bool neutral)
    {
        isNeutral = neutral;
        if (unitData != null)
            unitData.isNeutral = neutral;
    }

    /// <summary>
    /// Configure le cercle de sélection sous l'unité.
    /// </summary>
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
            sr.color = PlayerManager.GetPlayerColor(playerId);
        }
    }

    /// <summary>
    /// Positionne et valide la barre de vie de l'unité.
    /// </summary>
    private void InitHealthBar()
    {
        if (healthBar == null)
        {
            return;
        }
        healthBar.transform.localPosition = (1.1f * Vector3.up);
    }

    /// <summary>
    /// Mémorise la structure protectrice à prévenir en cas de mort.
    /// </summary>
    public void SetProtectorSourceStructure(StructureInstance sourceStructure)
    {
        protectorSourceStructure = sourceStructure;
    }

    /// <summary>
    /// Retire de la santé à l'unité et déclenche la mort si nécessaire.
    /// </summary>
    public void TakeDamage(float amount, UnitInstance attacker = null, int attackerPlayerId = -1)
    {
        if (unitData == null)
            return;

        lastAttackerPlayerId = attacker != null ? attacker.playerId : attackerPlayerId;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, unitData.maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Gère la destruction de l'unité et les effets de bord associés.
    /// </summary>
    void Die()
    {
        if (unitData != null && unitData.isProtector && protectorSourceStructure != null)
        {
            protectorSourceStructure.HandleProtectorDeath(this, lastAttackerPlayerId);
            protectorSourceStructure = null;
        }

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

    /// <summary>
    /// Ajoute ou retire de la santé tout en respectant les limites de l'unité.
    /// </summary>
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
