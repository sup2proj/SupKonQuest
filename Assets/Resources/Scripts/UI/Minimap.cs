using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Gère l'affichage de la minimap et déplace le cadre de vision de la caméra sur une interface isométrique.
/// </summary>
public class Minimap : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Minimap Settings")]
    [Tooltip("L'élément UI Image qui affiche le layout de la carte chargée.")]
    public Image mapImageDisplay;
    
    [Header("Viewport Settings")]
    [Tooltip("Le rectangle de visée représentant la caméra sur la minimap (doit être configuré en centre-milieu avec ancre 0,0,1,1).")]
    public RectTransform cameraViewport;
    
    // [Tooltip("Le sprite pour le cadre de visée (utilisez une bordure noire semi-transparente).")]
    // public Sprite viewportFrameSprite;

    private Vector2 mapWorldSize = new Vector2(64f, 64f);

    private Camera mainCamera;
    private RectTransform minimapRectTransform;
    private UnityEngine.UI.Outline viewportOutline;
    private bool isInitialized = false;

    void Start()
    {
        mainCamera = Camera.main;
        if (mapImageDisplay != null)
        {
            minimapRectTransform = mapImageDisplay.GetComponent<RectTransform>();
        }
        if (!string.IsNullOrEmpty(AutoLauncher.pendingMapFolder))
        {
            SetMapImage(AutoLauncher.pendingMapFolder);
        }
        else
        {
            Debug.LogWarning("[Minimap] pendingMapFolder vide. Chargement de la carte par défaut : TEST");
            // SetMapImage("TEST");
        }
        isInitialized = (mainCamera is not null && cameraViewport is not null && minimapRectTransform is not null);
    }

    void Update()
    {
        if (isInitialized)
        {
            UpdateViewportPosition();
        }
    }
    
    /// <summary>
    /// Calcule et projette la position 3D de la caméra sur l'espace UI du losange de la minimap.
    /// </summary>
    void UpdateViewportPosition()
    {
        float camAltitude = mainCamera.transform.position.y;
        float forwardY = mainCamera.transform.forward.y;
    
        Vector3 targetGroundPos = mainCamera.transform.position;
        if (Mathf.Abs(forwardY) > 0.001f)
        {
            float distanceToGround = camAltitude / Mathf.Abs(forwardY);
            targetGroundPos = mainCamera.transform.position + (mainCamera.transform.forward * distanceToGround);
        }

        float normalizedX = targetGroundPos.x / mapWorldSize.x;
        float normalizedY = targetGroundPos.z / mapWorldSize.y;

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        float minimapWidth = minimapRectTransform.rect.width;
        float minimapHeight = minimapRectTransform.rect.height;

        float localX = (normalizedX - 0.5f) * minimapWidth;
        float localY = (normalizedY - 0.5f) * minimapHeight;

        cameraViewport.anchoredPosition = new Vector2(localX, localY);

        float referenceOrthoSize = 2f; 
        Vector2 referenceViewportSize = new Vector2(30f, 20f);
    
        float zoomFactor = mainCamera.orthographicSize / referenceOrthoSize;
    
        cameraViewport.sizeDelta = referenceViewportSize * zoomFactor;
    }
    
    /// <summary>
    /// Charge l'image de layout depuis le dossier de ressources de la carte et calcule ses dimensions de texture.
    /// </summary>
    /// <param name="mapName">Nom exact du dossier de la carte.</param>
    public void SetMapImage(string mapName)
    {
        string resourcePath = "Maps/" + mapName + "/MapLayout";
        Texture2D loadedTexture = Resources.Load<Texture2D>(resourcePath);

        if (loadedTexture != null)
        {
            mapWorldSize = new Vector2(loadedTexture.width, loadedTexture.height);
            Debug.Log($"[Minimap] Taille du monde 3D synchronisée automatiquement sur la texture : {mapWorldSize.x}x{mapWorldSize.y}");

            Sprite newSprite = Sprite.Create(
                loadedTexture, 
                new Rect(0, 0, loadedTexture.width, loadedTexture.height), 
                new Vector2(0.5f, 0.5f)
            );
            if (mapImageDisplay != null)
            {
                mapImageDisplay.sprite = newSprite;
            }
        }
        else
        {
            Debug.LogError($"[Minimap] Impossible de charger le layout de carte requis à l'emplacement : Resources/{resourcePath}");
            if (mapImageDisplay != null) mapImageDisplay.sprite = null;
        }
    }
    
    /// <summary>
    /// Détecte le premier clic sur la minimap.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        HandleMinimapInput(eventData);
    }

    /// <summary>
    /// Détecte quand le joueur maintient le clic et glisse sa souris sur la minimap.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        HandleMinimapInput(eventData);
    }

    /// <summary>
    /// Convertit la position du clic en coordonnées 3D et déplace la caméra.
    /// </summary>
    private void HandleMinimapInput(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localCursor))
        {
            float normalizedX = (localCursor.x / minimapRectTransform.rect.width) + 0.5f;
            float normalizedY = (localCursor.y / minimapRectTransform.rect.height) + 0.5f;

            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            float targetWorldX = normalizedX * mapWorldSize.x;
            float targetWorldZ = normalizedY * mapWorldSize.y;

            CameraMouvement camMove = mainCamera.GetComponent<CameraMouvement>();
            if (camMove != null)
            {
                camMove.SetTargetFromMinimap(targetWorldX, targetWorldZ);
            }
        }
    }
    
}