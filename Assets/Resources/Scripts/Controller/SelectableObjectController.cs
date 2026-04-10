using UnityEngine;

public class SelectableObject : MonoBehaviour
{
    public GameObject SelectionMarker;
    public MeshRenderer MyMeshRenderer;
    public Material RedMat, GreenMat; 
    public bool IsSelected { get; private set; }

    private Color originalColor;

    private SpriteRenderer markerSprite;
    private bool buffIcons = false;
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
            if (unitInstance.unitData.type == UnitsType.Support)
            {
                InterfaceInstance.Instance.ShowSupportIcons();
                buffIcons = true;
            }
            else if (unitInstance.unitData.type == UnitsType.Healer)
            {
                InterfaceInstance.Instance.ShowHealerIcon();
                buffIcons = true;

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