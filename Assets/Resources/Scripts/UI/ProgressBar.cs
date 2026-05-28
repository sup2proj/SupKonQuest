using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float width = 0f;
    [SerializeField] private float height = 100f;

    [Header("References")]
    [SerializeField] private RectTransform progressBar;

    private float durationSeconds = 1f;
    private float elapsedSeconds = 0f;
    private float creationStartTime = 0f;
    private bool isRunning = false;



    /// <summary>
    /// Prépare la barre de progression au chargement.
    /// </summary>
    private void Awake()
    {
        WidthInitialized();
        SetNormalized(0f);
    }

    /// <summary>
    /// Rafraîchit l'affichage lorsque le composant est activé.
    /// </summary>
    private void OnEnable()
    {
        RefreshVisualFromClock();
    }

    /// <summary>
    /// Met à jour la progression tant que l'animation est en cours.
    /// </summary>
    private void Update()
    {
        if (!isRunning)
            return;

        RefreshVisualFromClock();
    }

    /// <summary>
    /// Démarre une nouvelle progression de création.
    /// </summary>
    public void StartCreation(float creationTimeSeconds)
    {
        StopCreation(resetToZero: true);

        WidthInitialized();

        if (creationTimeSeconds <= 0f)
        {
            durationSeconds = 0f;
            elapsedSeconds = 0f;
            creationStartTime = Time.time;
            isRunning = false;
            return;
        }

        durationSeconds = creationTimeSeconds;
        elapsedSeconds = 0f;
        creationStartTime = Time.time;
        isRunning = true;
        SetNormalized(0f);
    }

    /// <summary>
    /// Reprend une progression de création à partir du temps déjà écoulé.
    /// </summary>
    public void StartCreationFromElapsed(float creationTimeSeconds, float elapsedAlreadySeconds)
    {
        WidthInitialized();
        if (creationTimeSeconds <= 0f)
        {
            durationSeconds = 0f;
            elapsedSeconds = 0f;
            creationStartTime = Time.time;
            isRunning = false;
            SetNormalized(0f);
            return;
        }
        durationSeconds = creationTimeSeconds;
        elapsedSeconds = Mathf.Clamp(elapsedAlreadySeconds, 0f, durationSeconds);
        creationStartTime = Time.time - elapsedSeconds;
        isRunning = elapsedSeconds < durationSeconds;
        SetNormalized(durationSeconds > 0f ? elapsedSeconds / durationSeconds : 0f);
    }

    /// <summary>
    /// Arrête la progression de création en cours.
    /// </summary>
    public void StopCreation(bool resetToZero = false)
    {
        isRunning = false;

        if (resetToZero)
        {
            elapsedSeconds = 0f;
            SetNormalized(0f);
        }
    }

    /// <summary>
    /// Affiche ou masque visuellement la barre.
    /// </summary>
    public void SetFillVisible(bool visible)
    {
        if (progressBar == null)
            return;

        progressBar.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Synchronise l'affichage de la barre avec le temps écoulé réel.
    /// </summary>
    private void RefreshVisualFromClock()
    {
        if (durationSeconds <= 0f)
        {
            elapsedSeconds = 0f;
            SetNormalized(0f);
            return;
        }

        elapsedSeconds = Mathf.Clamp(Time.time - creationStartTime, 0f, durationSeconds);
        float t = elapsedSeconds / durationSeconds;
        SetNormalized(t);
    }

    /// <summary>
    /// Met à jour la largeur affichée à partir d'une valeur normalisée.
    /// </summary>
    private void SetNormalized(float t)
    {
        if (progressBar == null)
            return;
        t = Mathf.Clamp01(t);
        float newWidth = t * width;
        progressBar.sizeDelta = new Vector2(newWidth, height);
    }

    /// <summary>
    /// Détermine la largeur de référence de la barre si elle n'est pas définie.
    /// </summary>
    private void WidthInitialized()
    {
        if (progressBar == null)
            return;

        if (width > 0f)
            return;
        RectTransform parent = progressBar.parent as RectTransform;
        if (parent != null)
        {
            width = parent.rect.width;
        }
        if (width <= 0f)
        {
            width = progressBar.rect.width;
        }
    }
}