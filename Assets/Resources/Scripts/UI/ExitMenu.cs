using UnityEngine;
using UnityEngine.UI;

public class ExitMenu : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
    }

    public void Resume()
    {
        if (InterfaceInstance.Instance != null)
            InterfaceInstance.Instance.CloseExitMenu();
    }

    public void Quit()
    {
        if (InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.QuitGame();
        }
    }
}