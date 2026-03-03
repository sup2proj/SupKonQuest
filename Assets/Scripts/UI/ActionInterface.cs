using UnityEngine;

public class ActionInterface : MonoBehaviour
{
    public ActionButton[] buttons;

    void Start()
    {
        SetupButtons();
    }

    void SetupButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError("Aucun bouton n'est assigné dans l'Inspector!");
            return;
        }

        Debug.Log($"Nombre de boutons disponibles: {buttons.Length}");

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                int buttonNumber = i + 1;
                buttons[i].Setup(null, 0, () => ButtonAction(buttonNumber));
            }
            else
            {
                Debug.LogWarning($"Le bouton à l'index {i} est null!");
            }
        }
    }

    void ButtonAction(int buttonNumber)
    {
        Debug.Log(buttonNumber.ToString());
    }
}