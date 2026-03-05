using UnityEngine;

public class ActionInterface : MonoBehaviour
{
    public static ActionInterface Instance;

    [Header("Boutons pour Structure normale")]
    public ActionButton[] structureButtons;
    
    [Header("Boutons pour Harbour")]
    public ActionButton[] harbourButtons;
    
    [Header("Boutons pour Structure neutre")]
    public ActionButton[] neutralStructureButtons;

	[Header("Boutons pour déclancher les compétences des unités")]
    public ActionButton[] unitActionButtons;
    
    private StructureType currentStructureType;
    
    void Awake()
    {
        Debug.Log("[ActionInterface] Awake() appelé");
        Instance = this;
        HideAllButtons();
    }
    
    void Start()
    {
        Debug.Log("[ActionInterface] Start() appelé");
        SetupAllButtons();
    }

    void SetupAllButtons()
    {
        SetupButtonArray(structureButtons, "Structure");
        SetupButtonArray(harbourButtons, "Harbour");
        SetupButtonArray(neutralStructureButtons, "NeutralStructure");
    }

    void SetupButtonArray(ActionButton[] buttons, string typeName)
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning($"Aucun bouton n'est assigné pour {typeName}!");
            return;
        }

        Debug.Log($"Configuration de {buttons.Length} boutons pour {typeName}");

        StructureType typeEnum = StructureType.Structure;
        if (System.Enum.TryParse(typeName, out StructureType parsedType))
        {
            typeEnum = parsedType;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                int buttonNumber = i + 1;
                string btnTypeName = typeName;
                buttons[i].Setup(typeEnum, () => ButtonAction(buttonNumber, btnTypeName));
            }
        }
    }

    void ButtonAction(int buttonNumber, string structureTypeName)
    {
        Debug.Log($"[ActionInterface] ButtonAction appelé pour le bouton {buttonNumber} du type {structureTypeName}");
        // Ici vous pouvez ajouter la logique spécifique selon le type
    }
    
    public void HideAllButtons()
    {
        Debug.Log("[ActionInterface] HideAllButtons() appelé");
        HideButtonArray(structureButtons);
        HideButtonArray(harbourButtons);
        HideButtonArray(neutralStructureButtons);
		HideButtonArray(unitActionButtons);
    }

    void HideButtonArray(ActionButton[] buttons)
    {
        if (buttons == null) return;
        
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    void ShowButtonArray(ActionButton[] buttons)
    {
        if (buttons == null) return;
        
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(true);
                Debug.Log($"[ActionInterface] Bouton {button.name} activé");
            }
        }
    }
    
    public static void ShowStructureButtons(StructureType structureType)
    {
        if (Instance == null)
        {
            Debug.LogError("[ActionInterface] Instance est null! ShowStructureButtons ne peut pas fonctionner.");
            return;
        }

        Instance.currentStructureType = structureType;
        Instance.HideAllButtons();

        switch (structureType)
        {
            case StructureType.Structure:
                Debug.Log("[ActionInterface] Affichage des boutons pour Structure");
                Instance.ShowButtonArray(Instance.structureButtons);
                break;
            
            case StructureType.Harbour:
                Debug.Log("[ActionInterface] Affichage des boutons pour Harbour");
                Instance.ShowButtonArray(Instance.harbourButtons);
                break;
            
            case StructureType.NeutralStructure:
                Debug.Log("[ActionInterface] Affichage des boutons pour NeutralStructure");
                Instance.ShowButtonArray(Instance.neutralStructureButtons);
                break;
        }
    }
}