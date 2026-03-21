using UnityEngine;
using TMPro;

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
    
	[Header("Prices des unités (ordre identique aux unitDatas))")]
 	[SerializeField] private TextMeshProUGUI[] unitPriceTexts;
 	[SerializeField] public UnitData[] unitDatas;

	[Header("Image des structures")]
    public GameObject[] structureImage;


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
		SetupUnitPrices();	
    }

	public void SetupUnitPrices()
	{
		if (unitPriceTexts == null || unitDatas == null) return;

		int count = Mathf.Min(unitPriceTexts.Length, unitDatas.Length);
		for (int i = 0; i < count; i++)
		{
			if (unitPriceTexts[i] == null) continue;

			if (unitDatas[i] == null)
			{
				unitPriceTexts[i].text = "";
				continue;
			}


			float multiplier = (currentStructureType == StructureType.NeutralStructure) ? 1.20f : 1f;
			float finalPrice = unitDatas[i].price * multiplier;
			unitPriceTexts[i].text = finalPrice.ToString();
		}
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
            SendUnitsIcon(buttonNumber, false);
            
		}
         else if (structureTypeName == "NeutralStructure") {
            SendUnitsIcon(buttonNumber, true);
		}
         else if (structureTypeName == "Harbour") {
			HarbourButtonAction(buttonNumber);
		}
    }
	
	private void SendUnitsIcon(int buttonNumber, bool isPowered)
    {
        // Ajout à la queue UI: on transmet "l'image" du bouton cliqué (en pratique, on clone son GameObject)
        GameObject clickedImageGO = GetClickedUnitsIcon(buttonNumber, isPowered);
        if (clickedImageGO != null)
        {
            if (InterfaceInstance.Instance != null)
            {
                InterfaceInstance.Instance.addUnitToQueue(clickedImageGO);
				
                int unitIndex = buttonNumber - 1;
                UnitsType type = unitDatas[unitIndex].type;
                if (isPowered)
                {
                    InterfaceInstance.Instance.InitUnitsCreation(unitIndex, type, x, z, true);
                }
                else
                {
                    InterfaceInstance.Instance.InitUnitsCreation(unitIndex, type, x, z, false);
                }
            }
            else
            {
                Debug.LogWarning("[ActionInterface] InterfaceInstance.Instance est null, impossible d'ajouter à la queue.");
            }
        }
    }

    private GameObject GetClickedUnitsIcon(int buttonNumber, bool isPowered)
    {
        int index = buttonNumber - 1;
        if (isPowered)
        {
            return neutralStructureButtons[index].gameObject;
        }
        else
        {
            return structureButtons[index].gameObject;
        }
    }

    public void StructureButtonAction(int buttonNumber) {
		if (StructureManager.Instance == null)
		{
			Debug.LogError("[ActionInterface] StructureManager.Instance est null! Assurez-vous qu'un StructureManager existe dans la scène.");
			return;
		}
		Debug.Log("[ActionInterface] StructureButtonAction() appelé pour le bouton " + buttonNumber);
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
		HidePrices(unitPriceTexts);
		HideImage(structureImage);

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
            }
        }
    }

	void HidePrices(TextMeshProUGUI[] unitPriceTexts)
    {
        if (unitPriceTexts == null) return;
        
        foreach (var unitPriceText in unitPriceTexts)
        {
            if (unitPriceText != null)
            {
                unitPriceText.gameObject.SetActive(false);
            }
        }
    }

    void ShowPrices(TextMeshProUGUI[] unitPriceTexts)
    {
        if (unitPriceTexts == null) return;
        
        foreach (var unitPriceText in unitPriceTexts)
        {
            if (unitPriceText != null)
            {
                unitPriceText.gameObject.SetActive(true);
                unitPriceText.transform.SetAsLastSibling();
            }
        }
    }

	void HideImage(GameObject[] structureImage)
{
    if (structureImage == null) return;

    foreach (var img in structureImage)
    {
        if (img != null)
        {
            img.gameObject.SetActive(false);
        }
    }
}

    void ShowImage(GameObject[] structureImage, StructureType selectedType)
{
    if (structureImage == null) return;
    HideImage(structureImage);
    int index = (int)selectedType;
    if (index >= 0 && index < structureImage.Length && structureImage[index] != null)
    {
        structureImage[index].gameObject.SetActive(true);
    }
    else
    {
        Debug.LogWarning($"[ActionInterface] Aucune image configurée pour {selectedType} (index {index}).");
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
		Instance.SetupUnitPrices();

		switch (structureType)
		{
			case StructureType.Structure:
				Debug.Log("[ActionInterface] Affichage des boutons pour Structure");
				Instance.ShowButtonArray(Instance.structureButtons);
				Instance.ShowImage(Instance.structureImage, structureType);
				Instance.ShowPrices(Instance.unitPriceTexts);
				break;

			case StructureType.Harbour:
				Debug.Log("[ActionInterface] Affichage des boutons pour Harbour");
				Instance.ShowButtonArray(Instance.harbourButtons);
				Instance.ShowImage(Instance.structureImage, structureType);
				break;

			case StructureType.NeutralStructure:
				Debug.Log("[ActionInterface] Affichage des boutons pour NeutralStructure");
				Instance.ShowButtonArray(Instance.neutralStructureButtons);
				Instance.ShowImage(Instance.structureImage, structureType);
				Instance.ShowPrices(Instance.unitPriceTexts);
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
