using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class UnitInstance : NetworkBehaviour
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

    public NetworkVariable<int> netPlayerId = new NetworkVariable<int>(-1);
    public NetworkVariable<float> netMaxHealth = new NetworkVariable<float>(100f);
    public NetworkVariable<float> netHealth = new NetworkVariable<float>(100f);

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
    public override void OnDestroy()
    {
        base.OnDestroy(); 
        UnitsRegistry.Unregister(this);
    }

    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        netHealth.OnValueChanged += (oldValue, newValue) => 
        {
            currentHealth = newValue;
            if (healthBar != null) healthBar.SetHealth(currentHealth);
        };

        netPlayerId.OnValueChanged += (oldValue, newValue) => 
        {
            playerId = newValue;
            InitSelectionCircle(); 
        };

        if (NetworkManager.Singleton != null)
        {
            playerId = netPlayerId.Value;
            currentHealth = netHealth.Value;
        }

        ApplyRuntimePresentation();

        if (IsClient && !IsServer && healthBar != null)
        {
            healthBar.SetMaxHealth(netMaxHealth.Value);
            healthBar.SetHealth(currentHealth);
        }
    }

    /// <summary>
    /// Initialise aussi les visuels et l'enregistrement local quand aucun spawn réseau n'est utilisé.
    /// </summary>
    private void Start()
    {
        if (NetworkManager.Singleton == null || !IsSpawned)
            ApplyRuntimePresentation();
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
        
        if (IsSpawned && IsServer)
        {
            netPlayerId.Value = playerId;
            netMaxHealth.Value = unitData.maxHealth;
            netHealth.Value = currentHealth;
        }

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
        if (circleUnderFeet == null) return;

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
        if (healthBar == null) return;
        healthBar.transform.localPosition = (1.1f * Vector3.up);
    }

    /// <summary>
    /// Mémorise la structure protectrice à prévenir en cas de mort.
    /// </summary>
    public void SetProtectorSourceStructure(StructureInstance sourceStructure)
    {
        protectorSourceStructure = sourceStructure;
    }

    // <summary>
    /// Retire de la santé à l'unité et déclenche la mort si nécessaire.
    /// </summary>
    public void TakeDamage(float amount, UnitInstance attacker = null, int attackerPlayerId = -1)
    {
        if (unitData == null) return;

        if (NetworkManager.Singleton != null && IsClient && !IsServer) return;

        lastAttackerPlayerId = attacker != null ? attacker.playerId : attackerPlayerId;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, unitData.maxHealth);

        if (NetworkManager.Singleton != null && IsServer)
        {
            netHealth.Value = currentHealth;
        }

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
        if (Unity.Netcode.NetworkManager.Singleton != null && 
            Unity.Netcode.NetworkManager.Singleton.IsClient && 
            !Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            return; 
        }

        if (unitData != null && unitData.isProtector && protectorSourceStructure != null)
        {
            protectorSourceStructure.HandleProtectorDeath(this, lastAttackerPlayerId);
            protectorSourceStructure = null;
        }

        if (circleUnderFeet != null) Destroy(circleUnderFeet, 0f);
        if (healthBar != null) Destroy(healthBar, 0f);

        Unity.Netcode.NetworkObject netObj = GetComponent<Unity.Netcode.NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject, 0f);
        }

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
        if (unitData == null) return;
        
        if (NetworkManager.Singleton != null && IsClient && !IsServer) return;

        currentHealth += healthChange;
        currentHealth = Mathf.Clamp(currentHealth, 0f, unitData.maxHealth);

        if (NetworkManager.Singleton != null && IsServer)
        {
            netHealth.Value = currentHealth;
        }

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    /// <summary>
    /// Applique les états runtime partagés entre local et réseau.
    /// </summary>
    private void ApplyRuntimePresentation()
    {
        UnitsRegistry.Register(this, playerId);

        if (objectModel != null)
            objectModel.SetActive(false);

        InitSelectionCircle();
        InitHealthBar();
    }
}
