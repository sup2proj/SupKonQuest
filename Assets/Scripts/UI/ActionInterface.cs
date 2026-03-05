using UnityEngine;

public class ActionInterface : MonoBehaviour
{
    public static ActionInterface Instance;

    public ActionButton[] buttons;
    
    void Awake()
    {
        Debug.Log("[ActionInterface] Awake() appelé");
        Instance = this;
        HideStructureButtons();
    }
    
    void Start()
    {
        Debug.Log("[ActionInterface] Start() appelé");
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
                Debug.Log($"[ActionInterface] Configuration du bouton {buttonNumber} ({buttons[i].name})");
                buttons[i].Setup(() => ButtonAction(buttonNumber));
            }
            else
            {
                Debug.LogWarning($"Le bouton à l'index {i} est null!");
            }
        }
    }

    void ButtonAction(int buttonNumber)
    {
        Debug.Log($"[ActionInterface] ButtonAction appelé pour le bouton {buttonNumber}");
    }
    
    public void HideStructureButtons()
    {
        Debug.Log("[ActionInterface] HideStructureButtons() appelé");
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }
    }
    
    public static void ShowStructureButtons()
    {
        if (Instance == null)
        {
            Debug.LogError("[ActionInterface] Instance est null! ShowStructureButtons ne peut pas fonctionner.");
            return;
        }
        
        if (Instance.buttons == null || Instance.buttons.Length == 0)
        {
            Debug.LogError("[ActionInterface] Aucun bouton n'est assigné dans l'instance!");
            return;
        }
        
        foreach (var button in Instance.buttons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(true);
                Debug.Log($"[ActionInterface] Bouton {button.name} activé");
            }
        }
    }
}