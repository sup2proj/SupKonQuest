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
	private static float x;
	private static float z;
	private static StructureInstance currentSelectedStructure;
    
    void Awake()
    {
        Instance = this;
        HideAllButtons();
    }
    
    void Start()
    {
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
            return;
        }
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
		if (structureTypeName == "Structure") {
			StructureButtonAction(buttonNumber);
		}
         else if (structureTypeName == "NeutralStructure") {
			NeutralStructureButtonAction(buttonNumber);
		}
         else if (structureTypeName == "Harbour") {
			HarbourButtonAction(buttonNumber);
		}
	}

	public void StructureButtonAction(int buttonNumber) {
		if (StructureManager.Instance == null)
		{
			Debug.LogError("[ActionInterface] StructureManager.Instance est null! Assurez-vous qu'un StructureManager existe dans la scène.");
			return;
		}
		Debug.Log("[ActionInterface] StructureButtonAction() appelé pour le bouton " + buttonNumber);
		switch(buttonNumber)
		{
			case 1:
                Debug.Log("Action 1 (Antiblindage) pour Structure exécutée");
				StructureManager.Instance.SpawnUnitByTypeAtPosition(UnitsType.AntiBlindage, x, z);
                break;
            case 2:
                Debug.Log("Action 2 (Archer) pour Structure exécutée");
				StructureManager.Instance.SpawnUnitByTypeAtPosition(UnitsType.Archer, x, z);
                break;
            case 3:
                Debug.Log("Action 3 (Healer) pour Structure exécutée");
				StructureManager.Instance.SpawnUnitByTypeAtPosition(UnitsType.Healer, x, z);
                break;
			case 4:
                Debug.Log("Action 4 (Heavy) pour Structure exécutée");
				StructureManager.Instance.SpawnUnitByTypeAtPosition(UnitsType.Heavy, x, z);
                break;
			case 5:
                Debug.Log("Action 5 (infantry) pour Structure exécutée");
				StructureManager.Instance.SpawnUnitByTypeAtPosition(UnitsType.Infantry, x, z);
                break;
			case 6:
                Debug.Log("Action 6 (mortar) pour Structure exécutée");
				StructureManager.Instance.SpawnUnitByTypeAtPosition(UnitsType.Mortar, x, z);
                break;
			case 7:
                Debug.Log("Action 7 (support) pour Structure exécutée");
				StructureManager.Instance.SpawnUnitByTypeAtPosition(UnitsType.Support, x, z);
                break;
            default:
                Debug.LogWarning($"Aucune action définie pour le bouton {buttonNumber} du type Structure");
                break;
        }
    }

	public void NeutralStructureButtonAction(int buttonNumber) {
        switch(buttonNumber)
        {
            case 1:
                Debug.Log("Action 1 pour NeutralStructure exécutée");
                // Implémentez ici l'action spécifique pour le bouton 1
                break;
            case 2:
                Debug.Log("Action 2 pour NeutralStructure exécutée");
                // Implémentez ici l'action spécifique pour le bouton 2
                break;
            case 3:
                Debug.Log("Action 3 pour NeutralStructure exécutée");
                // Implémentez ici l'action spécifique pour le bouton 3
                break;
            default:
                Debug.LogWarning($"Aucune action définie pour le bouton {buttonNumber} du type NeutralStructure");
                break;
        }
	}

	public void HarbourButtonAction(int buttonNumber) {
        switch(buttonNumber)
        {
            case 1:
                Debug.Log("Action 1 pour Harbour exécutée");
                // Implémentez ici l'action spécifique pour le bouton 1
                break;
            case 2:
                Debug.Log("Action 2 pour Harbour exécutée");
                // Implémentez ici l'action spécifique pour le bouton 2
                break;
            case 3:
                Debug.Log("Action 3 pour Harbour exécutée");
                // Implémentez ici l'action spécifique pour le bouton 3
                break;
            default:
                Debug.LogWarning($"Aucune action définie pour le bouton {buttonNumber} du type Harbour");
                break;
        }
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

    public static void SetSelectedStructure(StructureInstance structure, Vector3 position)
    {
        currentSelectedStructure = structure;
        x = position.x;
        z = position.z;
        Debug.Log($"[ActionInterface] Structure sélectionnée aux coordonnées x={position.x}, z={position.z}");
    }
}