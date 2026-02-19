using UnityEngine;
using UnityEngine.InputSystem;

public class PorteurMortierInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsData unitData;
    private float currentHealth;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (unitData != null)
        {
            currentHealth = unitData.maxHealth;
        }

    }
    void Update()
    {
        bool isMoving = Keyboard.current.spaceKey.isPressed;
        Move(isMoving);
    }

    public void Move(bool isMoving)
    {
        animator.SetBool("isMoving", isMoving);
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
