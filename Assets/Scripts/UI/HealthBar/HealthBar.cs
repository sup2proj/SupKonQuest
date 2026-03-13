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
    private float health = 1f;

    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        // Garder la vie cohérente quand le max change
        health = Mathf.Clamp(health, 0f, maxHealth);
        Refresh();
    }

    public void SetHealth(float value)
    {
        health = Mathf.Clamp(value, 0f, maxHealth);
        Refresh();
    }

    private void Refresh()
    {
        if (healthBar == null)
            return;

        // Définit aussi la hauteur une seule fois via sizeDelta
        float ratio = maxHealth <= 0f ? 0f : (health / maxHealth);
        float newWidth = ratio * width;
        healthBar.sizeDelta = new Vector2(newWidth, height);
    }
}
