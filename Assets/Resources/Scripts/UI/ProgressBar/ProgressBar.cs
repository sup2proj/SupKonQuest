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



    private void Awake()
    {
        WidthInitialized();
        SetNormalized(0f);
    }

    private void OnEnable()
    {
        RefreshVisualFromClock();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        RefreshVisualFromClock();
    }

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

    public void StopCreation(bool resetToZero = false)
    {
        isRunning = false;

        if (resetToZero)
        {
            elapsedSeconds = 0f;
            SetNormalized(0f);
        }
    }

    public bool IsFinished()
    {
        if (durationSeconds <= 0f)
            return true;

        if (!isRunning)
            return elapsedSeconds >= durationSeconds;

        return (Time.time - creationStartTime) >= durationSeconds;
    }

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