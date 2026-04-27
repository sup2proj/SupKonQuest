using UnityEngine;

public class SelectableObject : MonoBehaviour
{
    public GameObject SelectionMarker;
    public MeshRenderer MyMeshRenderer;
    public Material RedMat, GreenMat; 
    public bool IsSelected { get; private set; }

    private Color originalColor;

    private SpriteRenderer markerSprite;
    private void OnEnable()
    {
        if (SelectionManager.Instance != null)
            SelectionManager.Instance.RegisterSelectable(this);
    }

    private void Start()
    {
        markerSprite = SelectionMarker.GetComponent<SpriteRenderer>();
        if (markerSprite != null)
        {
            originalColor = markerSprite.color;
        }
    }

    public void SelectMe()
    {
        UnitInstance unitInstance = gameObject.GetComponent<UnitInstance>();
        int playerUnitsId = unitInstance.playerId;
        int playerId = PlayerManager.Instance.GetActivePlayerId();
        if (playerId == playerUnitsId)
        {
            IsSelected = true;
            if (markerSprite != null)
                markerSprite.color = Color.green;
            if (unitInstance.unitData.type == UnitsType.Support && !Spells.Instance.IsSpellOnCooldown)
            {
                InterfaceInstance.Instance.ShowSupportIcons();
            }
            else if (unitInstance.unitData.type == UnitsType.Healer && !Spells.Instance.IsSpellOnCooldown)
            {
                InterfaceInstance.Instance.ShowHealerIcon();
            }
        }
    }
    
    public void DeselectMe()
    {
        Debug.Log("Deselected: " + gameObject.name);
        IsSelected = false;
        if (markerSprite != null)
            markerSprite.color = originalColor;
    }
}