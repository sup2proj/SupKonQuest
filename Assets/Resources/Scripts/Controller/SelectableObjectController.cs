using UnityEngine;

public class SelectableObject : MonoBehaviour
{
    public GameObject SelectionMarker;
    public bool IsSelected { get; private set; }

    private Color originalColor;

    private SpriteRenderer markerSprite;
    /// <summary>
    /// Enregistre cet objet auprès du SelectionManager lorsque l'objet est activé.
    /// </summary>
    private void OnEnable()
    {
        if (SelectionManager.Instance != null)
            SelectionManager.Instance.RegisterSelectable(this);
    }

    /// <summary>
    /// Initialise la référence au SpriteRenderer du marqueur de sélection et conserve sa couleur d'origine.
    /// </summary>
    private void Start()
    {
        markerSprite = SelectionMarker.GetComponent<SpriteRenderer>();
        if (markerSprite != null)
        {
            originalColor = markerSprite.color;
        }
    }

    /// <summary>
    /// Sélectionne cette unité si elle appartient au joueur actif et affiche les icônes d'interface pertinentes.
    /// </summary>
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
    
    /// <summary>
    /// Désélectionne cette unité et restaure le visuel du marqueur ainsi que l'interface associée.
    /// </summary>
    public void DeselectMe()
    {
        if (this == null || gameObject == null) return;
        Debug.Log("Deselected: " + gameObject.name);
        IsSelected = false;
        if (markerSprite != null)
            markerSprite.color = originalColor;
        InterfaceInstance.Instance.hideBuffIcons();
        InterfaceInstance.Instance.HideBoatExitIcons();
    }
}