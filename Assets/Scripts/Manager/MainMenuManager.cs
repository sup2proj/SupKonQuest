using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public TextMeshProUGUI[] buttons;

    public float normalSize   = 24f;
    public float selectedSize = 36f;

    private int currentIndex = 0;

    void Start()
    {
        UpdateSizes();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = currentIndex + 1;
            if (currentIndex > buttons.Length - 1)
                currentIndex = 0;
            UpdateSizes();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = currentIndex - 1;
            if (currentIndex < 0)
                currentIndex = buttons.Length - 1;
            UpdateSizes();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Confirm();
        }
    }

    void UpdateSizes()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == currentIndex)
                buttons[i].fontSize = selectedSize;
            else
                buttons[i].fontSize = normalSize;
        }
    }

    void Confirm()
    {
        if (currentIndex == 0) NewGame();
        if (currentIndex == 1) Continue();
        if (currentIndex == 2) Options();
        if (currentIndex == 3) Quit();
    }

    public void NewGame()  { SceneManager.LoadScene("GameScene"); }
    public void Continue() { SceneManager.LoadScene("GameScene"); }
    public void Options()  { SceneManager.LoadScene("Options");   }
    public void Quit()     { Application.Quit();                  }
}
