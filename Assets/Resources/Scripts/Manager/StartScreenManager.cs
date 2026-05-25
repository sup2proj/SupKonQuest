using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gère l'écran de démarrage initial du jeu, affiche un texte clignotant et attend que le joueur presse une touche pour charger le menu principal.
/// </summary>
public class StartScreenManager : MonoBehaviour
{
    public TextMeshProUGUI pressKeyText;

    private bool isBlinking = true;

    void Start()
    {
        StartCoroutine(Blink());
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            isBlinking = false;
            SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// Coroutine qui crée un effet de clignotement sur le texte d'invite en alternant sa visibilité à intervalles réguliers (0.7s allumé, 0.4s éteint).
    /// </summary>
    IEnumerator Blink()
    {
        while (isBlinking)
        {
            pressKeyText.enabled = true;
            yield return new WaitForSeconds(0.7f);
            pressKeyText.enabled = false;
            yield return new WaitForSeconds(0.4f);
        }
    }
}
