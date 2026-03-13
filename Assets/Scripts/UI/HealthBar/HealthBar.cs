using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float width = 100f;
    [SerializeField] private float height = 100f;

    [Header("References")]
    [SerializeField] private RectTransform healthBar;

    private float maxHealth = 1f;
    private float currentHealth = 1f;

    private void Awake()
    {
        Refresh();
    }

    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        Refresh();
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        Refresh();
    }

    private void Refresh()
    {
        if (healthBar == null)
            return;

        float ratio = maxHealth <= 0f ? 0f : (currentHealth / maxHealth);
        float newWidth = ratio * width;
        healthBar.sizeDelta = new Vector2(newWidth, height);
    }
}
