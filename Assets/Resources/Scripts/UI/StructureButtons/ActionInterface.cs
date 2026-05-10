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
 	[SerializeField] private TextMeshProUGUI[] boatPriceTexts;
    [SerializeField] private TextMeshProUGUI[] unitProtectorPriceTexts;
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
        GameObject clickedImageGO = GetClickedUnitsIcon(buttonNumber, isPowered);
        if (clickedImageGO != null)
        {
            if (InterfaceInstance.Instance != null)
            {
                int unitIndex = buttonNumber - 1;
                UnitsType type = unitDatas[unitIndex].type;

                bool accepted;
                if (isPowered)
                {
                    accepted = InterfaceInstance.Instance.InitUnitsCreation(unitIndex, type, x, z, true, false);
                }
                else
                {
                    accepted = InterfaceInstance.Instance.InitUnitsCreation(unitIndex, type, x, z, false, false);
                }

                if (accepted)
                {
                    InterfaceInstance.Instance.addUnitToQueue(clickedImageGO);
                }
                else
                {
                    Debug.Log($"[ActionInterface] Création refusée (pas assez d'or ?) -> icône non ajoutée à la queue. unitIndex={unitIndex}, type={type}");
                }
            }
            else
            {
                Debug.LogWarning("[ActionInterface] InterfaceInstance.Instance est null, impossible d'ajouter à la queue.");
            }
        }
    }

    public GameObject GetClickedUnitsIcon(int buttonNumber, bool isPowered)
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

    public void HarbourButtonAction(int buttonNumber) {
        int index = buttonNumber - 1;
        Debug.Log("[ActionInterface] HarbourButtonAction appelé pour buttonNumber=" + buttonNumber + " (index=" + index + ")");
        if (index < 0 || unitDatas == null || index >= unitDatas.Length)
        {
            Debug.LogWarning($"[ActionInterface] HarbourButtonAction: index invalide {index}");
            return;
        }

        GameObject clickedImageGO = null;
        if (harbourButtons != null && index < harbourButtons.Length && harbourButtons[index] != null)
            clickedImageGO = harbourButtons[index].gameObject;

        if (clickedImageGO != null)
        {
            if (InterfaceInstance.Instance != null)
            {
                int unitIndex = index + 7; // Décalage de 7 pour accéder aux unités navales dans unitDatas
                UnitsType type = unitDatas[unitIndex].type;
                Debug.Log(type);
                bool accepted = InterfaceInstance.Instance.InitUnitsCreation(unitIndex, type, x, z, false, false);
                if (accepted)
                {
                    InterfaceInstance.Instance.addUnitToQueue(clickedImageGO);
                }
                else
                {
                    Debug.Log($"[ActionInterface] Création refusée (pas assez d'or ?) -> icône non ajoutée à la queue. unitIndex={unitIndex}, type={type}");
                }
            }
            else
            {
                Debug.LogWarning("[ActionInterface] InterfaceInstance.Instance est null, impossible d'ajouter à la queue (Harbour)." );
            }
        }
        else
        {
            Debug.LogWarning($"[ActionInterface] HarbourButtonAction: pas d'icône configurée pour le bouton {buttonNumber}");
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
        foreach (var unitProtectorPriceText in unitProtectorPriceTexts)
        {
            if (unitProtectorPriceText != null)
            {
                unitProtectorPriceText.gameObject.SetActive(false);
            }
        }

        foreach (var boatPriceText in boatPriceTexts)
        {
            if (boatPriceText != null)
            {
                boatPriceText.gameObject.SetActive(false);
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
    
    void ShowBoatPrices(TextMeshProUGUI[] boatPriceTexts)
    {
        if (boatPriceTexts == null) return;
        
        foreach (var boatPriceText in boatPriceTexts)
        {
            if (boatPriceText != null)
            {
                boatPriceText.gameObject.SetActive(true);
                boatPriceText.transform.SetAsLastSibling();
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
                Instance.ShowBoatPrices(Instance.boatPriceTexts);
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

    public void ShowUnitProtectorPrice(int unitIndex,  int priceIndex)
    {
        if (unitProtectorPriceTexts == null || unitDatas == null) return;
        
        float multiplier = (currentStructureType == StructureType.NeutralStructure) ? 1.20f : 1f;
        float finalPrice = unitDatas[unitIndex].price * multiplier;
        unitProtectorPriceTexts[priceIndex].text = finalPrice.ToString();
        unitProtectorPriceTexts[priceIndex].gameObject.SetActive(true);
    }
}
