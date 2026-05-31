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

    /// <summary>
    /// Ferme le menu de sortie et reprend le jeu.
    /// </summary>
    public void Resume()
    {
        if (InterfaceInstance.Instance != null)
            InterfaceInstance.Instance.CloseExitMenu();
    }

    /// <summary>
    /// Quitte le jeu et revient au menu principal.
    /// </summary>
    public void Quit()
    {
        if (InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.QuitGame();
        }
    }
}