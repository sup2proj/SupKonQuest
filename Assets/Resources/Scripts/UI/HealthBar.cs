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

    /// <summary>
    /// Réinitialise l'affichage de la barre de vie au chargement.
    /// </summary>
    private void Awake()
    {
        Refresh();
    }

    /// <summary>
    /// Définit la santé maximale et recadre la santé courante.
    /// </summary>
    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        Refresh();
    }

    /// <summary>
    /// Met à jour la santé courante affichée par la barre.
    /// </summary>
    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        Refresh();
    }

    /// <summary>
    /// Recalcule la largeur visuelle de la barre selon le ratio de santé.
    /// </summary>
    private void Refresh()
    {
        if (healthBar == null)
            return;

        float ratio = maxHealth <= 0f ? 0f : (currentHealth / maxHealth);
        float newWidth = ratio * width;
        healthBar.sizeDelta = new Vector2(newWidth, height);
    }
}
