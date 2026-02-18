using UnityEngine;

public class UnitInstance : MonoBehaviour
{
    private UnitsData unitData;

    public void Init(UnitsData data)
    {
        unitData = data;
    }

    public void TakeDamage(float damage)
    {
        if (unitData != null)
        {
            unitData.health -= damage;
            if (unitData.health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public float GetHealth()
    {
        return unitData != null ? unitData.health : 0;
    }

    public float GetMaxHealth()
    {
        return unitData != null ? unitData.maxHealth : 0;
    }
}