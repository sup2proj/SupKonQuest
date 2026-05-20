using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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
