using System.Collections;
using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float width = 0f;
    [SerializeField] private float height = 100f;

    [Header("References")]
    [SerializeField] private RectTransform progressBar;

    private Coroutine routine;

    private float durationSeconds = 1f;
    private float elapsedSeconds = 0f;



    private void Awake()
    {
        WidthInitialized();
        SetNormalized(0f);
    }

    public void StartCreation(float creationTimeSeconds)
    {
        StopCreation(resetToZero: true);

        WidthInitialized();

        if (creationTimeSeconds <= 0f)
        {
            durationSeconds = 0f;
            elapsedSeconds = 0f;
            return;
        }

        durationSeconds = creationTimeSeconds;
        elapsedSeconds = 0f;
        SetNormalized(0f);
        routine = StartCoroutine(FillOverTime());
    }

    public void StopCreation(bool resetToZero = false)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (resetToZero)
        {
            elapsedSeconds = 0f;
            SetNormalized(0f);
        }
    }

    public bool IsFinished()
    {
        // Fini quand on a atteint (ou dépassé) 100%.
        if (durationSeconds <= 0f)
            return true;

        return elapsedSeconds >= durationSeconds;
    }

    private IEnumerator FillOverTime()
    {
        while (elapsedSeconds < durationSeconds)
        {
            elapsedSeconds += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            SetNormalized(t);
            yield return null;
        }

        // À 100%, on revient automatiquement à 0%.
        SetNormalized(1f);
        StopCreation(resetToZero: true);
    }

    private void SetNormalized(float t)
    {
        if (progressBar == null)
            return;
        t = Mathf.Clamp01(t);
        float newWidth = t * width;
        progressBar.sizeDelta = new Vector2(newWidth, height);
    }

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