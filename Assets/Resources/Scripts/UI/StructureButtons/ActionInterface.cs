using UnityEngine;
using TMPro;

public class ActionInterface : MonoBehaviour
{
    private const int HarbourUnitDataOffset = 7;
    private const float PoweredStructureMultiplier = 1.20f;

    public static ActionInterface Instance;

    [Header("Boutons pour Structure normale")]
    public ActionButton[] structureButtons;

    [Header("Boutons pour Harbour")]
    public ActionButton[] harbourButtons;

    [Header("Boutons pour Structure neutre")]
    public ActionButton[] neutralStructureButtons;

    [Header("Boutons pour declancher les competences des unites")]
    public ActionButton[] unitActionButtons;

    [Header("Prices des unites (ordre identique aux unitDatas))")]
    [SerializeField] private TextMeshProUGUI[] unitPriceTexts;
    [SerializeField] private TextMeshProUGUI[] boatPriceTexts;
    [SerializeField] private TextMeshProUGUI[] unitProtectorPriceTexts;
    [SerializeField] public UnitData[] unitDatas;

    [Header("Image des structures")]
    public GameObject[] structureImage;

    private StructureType currentStructureType;
    private static float x;
    private static float z;

    void Awake()
    {
        Instance = this;
        HideAllButtons();
    }

    void Start()
    {
        SetupAllButtons();
        SetupUnitPrices();
        SetupBoatPrices();
    }

    public void SetupUnitPrices()
    {
        SetupPriceTexts(unitPriceTexts, 0, GetCurrentUnitPriceMultiplier());
    }

    public void SetupBoatPrices()
    {
        SetupPriceTexts(boatPriceTexts, HarbourUnitDataOffset, 1f);
    }

    private void SetupAllButtons()
    {
        SetupButtonArray(structureButtons, StructureType.Structure);
        SetupButtonArray(harbourButtons, StructureType.Harbour);
        SetupButtonArray(neutralStructureButtons, StructureType.NeutralStructure);
    }

    private void SetupButtonArray(ActionButton[] buttons, StructureType structureType)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            ActionButton button = buttons[i];
            if (button == null)
                continue;

            int buttonNumber = i + 1;
            button.Setup(structureType, () => ButtonAction(buttonNumber, structureType));
        }
    }

    private void ButtonAction(int buttonNumber, StructureType structureType)
    {
        switch (structureType)
        {
            case StructureType.Structure:
                SendUnitsIcon(buttonNumber, false);
                break;

            case StructureType.NeutralStructure:
                SendUnitsIcon(buttonNumber, true);
                break;

            case StructureType.Harbour:
                HarbourButtonAction(buttonNumber);
                break;
        }
    }

    private void SendUnitsIcon(int buttonNumber, bool isPowered)
    {
        ActionButton[] sourceButtons = isPowered ? neutralStructureButtons : structureButtons;
        TryCreateUnitFromButton(buttonNumber, sourceButtons, 0, isPowered, "Structure");
    }

    public GameObject GetClickedUnitsIcon(int buttonNumber, bool isPowered)
    {
        ActionButton[] sourceButtons = isPowered ? neutralStructureButtons : structureButtons;
        int index = buttonNumber - 1;

        if (!IsValidIndex(sourceButtons, index) || sourceButtons[index] == null)
            return null;

        return sourceButtons[index].gameObject;
    }

    public void HarbourButtonAction(int buttonNumber)
    {
        TryCreateUnitFromButton(buttonNumber, harbourButtons, HarbourUnitDataOffset, false, "Harbour");
    }

    private bool TryCreateUnitFromButton(int buttonNumber, ActionButton[] sourceButtons, int unitDataOffset, bool isPowered, string logContext)
    {
        int buttonIndex = buttonNumber - 1;
        if (!IsValidIndex(sourceButtons, buttonIndex) || sourceButtons[buttonIndex] == null)
        {
            Debug.LogWarning($"[ActionInterface] {logContext}: pas d'icone configuree pour le bouton {buttonNumber}");
            return false;
        }

        int unitIndex = buttonIndex + unitDataOffset;
        if (!TryGetUnitData(unitIndex, logContext, out UnitData data))
            return false;

        if (InterfaceInstance.Instance == null)
        {
            Debug.LogWarning($"[ActionInterface] InterfaceInstance.Instance est null, creation impossible ({logContext}).");
            return false;
        }

        bool accepted = InterfaceInstance.Instance.InitUnitsCreation(unitIndex, data.type, x, z, isPowered, false);
        if (!accepted)
        {
            Debug.Log($"[ActionInterface] Creation refusee -> icone non ajoutee. unitIndex={unitIndex}, type={data.type}");
            return false;
        }

        InterfaceInstance.Instance.addUnitToQueue(sourceButtons[buttonIndex].gameObject);
        return true;
    }

    public void HideAllButtons()
    {
        Debug.Log("[ActionInterface] HideAllButtons() appele");
        SetButtonsActive(false, structureButtons, harbourButtons, neutralStructureButtons, unitActionButtons);
        SetTextsActive(false, unitPriceTexts, boatPriceTexts, unitProtectorPriceTexts);
        SetImagesActive(false, structureImage);
    }

    private void SetButtonsActive(bool active, params ActionButton[][] buttonGroups)
    {
        if (buttonGroups == null)
            return;

        for (int i = 0; i < buttonGroups.Length; i++)
        {
            ActionButton[] buttons = buttonGroups[i];
            if (buttons == null)
                continue;

            for (int j = 0; j < buttons.Length; j++)
            {
                if (buttons[j] != null)
                    buttons[j].gameObject.SetActive(active);
            }
        }
    }

    private void SetTextsActive(bool active, params TextMeshProUGUI[][] textGroups)
    {
        if (textGroups == null)
            return;

        for (int i = 0; i < textGroups.Length; i++)
        {
            TextMeshProUGUI[] texts = textGroups[i];
            if (texts == null)
                continue;

            for (int j = 0; j < texts.Length; j++)
            {
                if (texts[j] == null)
                    continue;

                texts[j].gameObject.SetActive(active);
                if (active)
                    texts[j].transform.SetAsLastSibling();
            }
        }
    }

    private void SetImagesActive(bool active, GameObject[] images)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
                images[i].SetActive(active);
        }
    }

    private void ShowImage(StructureType selectedType)
    {
        SetImagesActive(false, structureImage);

        int index = (int)selectedType;
        if (IsValidIndex(structureImage, index) && structureImage[index] != null)
        {
            structureImage[index].SetActive(true);
            return;
        }

        Debug.LogWarning($"[ActionInterface] Aucune image configuree pour {selectedType} (index {index}).");
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
        Instance.SetupBoatPrices();
        Instance.ShowImage(structureType);
        Instance.ShowButtonsAndPricesFor(structureType);
    }

    private void ShowButtonsAndPricesFor(StructureType structureType)
    {
        switch (structureType)
        {
            case StructureType.Structure:
                Debug.Log("[ActionInterface] Affichage des boutons pour Structure");
                SetButtonsActive(true, structureButtons);
                SetTextsActive(true, unitPriceTexts);
                break;

            case StructureType.Harbour:
                Debug.Log("[ActionInterface] Affichage des boutons pour Harbour");
                SetButtonsActive(true, harbourButtons);
                SetTextsActive(true, boatPriceTexts);
                break;

            case StructureType.NeutralStructure:
                Debug.Log("[ActionInterface] Affichage des boutons pour NeutralStructure");
                SetButtonsActive(true, neutralStructureButtons);
                SetTextsActive(true, unitPriceTexts);
                break;
        }
    }

    public static void SetSelectedStructure(StructureInstance structure, Vector3 position)
    {
        x = position.x;
        z = position.z;
        string structureName = structure != null ? structure.name : "null";
        Debug.Log($"[ActionInterface] Structure selectionnee: {structureName}, x={position.x}, z={position.z}");
    }

    public void ShowUnitProtectorPrice(int unitIndex, int priceIndex)
    {
        if (!IsValidIndex(unitProtectorPriceTexts, priceIndex))
            return;

        if (!TryGetUnitData(unitIndex, "Protector", out UnitData data))
            return;

        TextMeshProUGUI priceText = unitProtectorPriceTexts[priceIndex];
        if (priceText == null)
            return;

        priceText.text = FormatPrice(data.price * GetCurrentUnitPriceMultiplier());
        priceText.gameObject.SetActive(true);
        priceText.transform.SetAsLastSibling();
    }

    private void SetupPriceTexts(TextMeshProUGUI[] priceTexts, int unitDataOffset, float multiplier)
    {
        if (priceTexts == null || unitDatas == null)
            return;

        for (int i = 0; i < priceTexts.Length; i++)
        {
            TextMeshProUGUI priceText = priceTexts[i];
            if (priceText == null)
                continue;

            int unitIndex = i + unitDataOffset;
            if (!IsValidIndex(unitDatas, unitIndex) || unitDatas[unitIndex] == null)
            {
                priceText.text = "";
                continue;
            }

            priceText.text = FormatPrice(unitDatas[unitIndex].price * multiplier);
        }
    }

    private bool TryGetUnitData(int unitIndex, string logContext, out UnitData data)
    {
        data = null;
        if (!IsValidIndex(unitDatas, unitIndex) || unitDatas[unitIndex] == null)
        {
            Debug.LogWarning($"[ActionInterface] {logContext}: unitData invalide index={unitIndex}");
            return false;
        }

        data = unitDatas[unitIndex];
        return true;
    }

    private float GetCurrentUnitPriceMultiplier()
    {
        return currentStructureType == StructureType.NeutralStructure ? PoweredStructureMultiplier : 1f;
    }

    private string FormatPrice(float price)
    {
        return Mathf.RoundToInt(price).ToString();
    }

    private bool IsValidIndex<T>(T[] array, int index)
    {
        return array != null && index >= 0 && index < array.Length;
    }
}
