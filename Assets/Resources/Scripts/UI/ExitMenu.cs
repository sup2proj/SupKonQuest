using UnityEngine;
using UnityEngine.UI;

public class ExitMenu : MonoBehaviour
{
    [SerializeField] private Button resumeButton;

    private void Awake()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
    }

    public void Resume()
    {
        if (InterfaceInstance.Instance != null)
            InterfaceInstance.Instance.CloseExitMenu();
    }
}