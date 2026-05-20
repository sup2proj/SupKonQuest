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
        if (Defeat.IsPlayerDefeated(playerId))
            return;

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
            else if (unitInstance.unitData.type == UnitsType.Destroyer || unitInstance.unitData.type == UnitsType.Fregate || unitInstance.unitData.type == UnitsType.Transport)
            {
                InterfaceInstance.Instance.ShowBoatExitIcons();
            }
        }
    }
    
    public void DeselectMe()
    {
        Debug.Log("Deselected: " + gameObject.name);
        IsSelected = false;
        if (markerSprite != null)
            markerSprite.color = originalColor;
        InterfaceInstance.Instance.hideBuffIcons();
        InterfaceInstance.Instance.HideBoatExitIcons();
    }
}