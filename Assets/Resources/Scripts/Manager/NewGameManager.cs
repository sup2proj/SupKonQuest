using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class NewGameManager : MonoBehaviour
{
    public TextMeshProUGUI[] buttons;

    public float normalSize = 50f;
    public float selectedSize = 60f;

    private int currentIndex = 0;

    void Start()
    {
        UpdateSizes();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentIndex = currentIndex + 1;
            if (currentIndex > buttons.Length - 1)
                currentIndex = 0;
            UpdateSizes();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
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
        if (currentIndex == 0) PreparingLocalGame();
        if (currentIndex == 1) PreparingMultiGame();
        if (currentIndex == 2) Back();
    }

    public void PreparingLocalGame()  { SceneManager.LoadScene("LaunchingLocalNewGame"); }
    public void PreparingMultiGame() { SceneManager.LoadScene("GameScene"); }
    public void Back()  { SceneManager.LoadScene("MainMenu");   }
}
