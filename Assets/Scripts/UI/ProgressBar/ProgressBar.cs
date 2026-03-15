using UnityEngine;

public class ProgressBar : MonoBehaviour
{

    [Header("Config")]
    [SerializeField] private float width = 0f;
    [SerializeField] private float height = 100f;

    [Header("References")]
    [SerializeField] private RectTransform progressBar;

    private float maxProgression = 1f;
    private float currentProgression = 1f;

    private void Awake()
    {
        Refresh();
    }

    public void SetProgressionBarEmpty(float value)
    {
        maxProgression = Mathf.Max(1f, value);
        currentProgression = Mathf.Clamp(currentProgression, 0f, maxProgression);
        Refresh();
    }

    public void SetHealth(float value)
    {
        currentProgression = Mathf.Clamp(value, 0f, maxProgression);
        Refresh();
    }

    private void Refresh()
    {
        if (progressBar == null)
            return;

        float ratio = maxProgression <= 0f ? 0f : (currentProgression / maxProgression);
        float newWidth = ratio * width;
        progressBar.sizeDelta = new Vector2(newWidth, height);
    }
}
