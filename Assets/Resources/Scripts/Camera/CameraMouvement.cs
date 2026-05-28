using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMouvement : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public int boundary = 30;
    public bool isAZERTY = true;

    [Header("Map Data")]
    private int mapWidth;
    private int mapHeight;

    private Vector3 targetPosition;
    private Camera cam;

    /// <summary>
    /// Initialise la caméra en mode orthographique et positionne la targetPosition par défaut.
    /// </summary>
    void Start()
    {
        cam = Camera.main;
        if (cam == null) cam = GetComponent<Camera>();

        cam.orthographic = true;
        cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);

        targetPosition = cam.transform.position;
        targetPosition.y = 20f; 
    }
    /// <summary>
    /// Effectue le traitement par frame : déplacement, zoom et application des limites, puis applique la position à la caméra.
    /// </summary>
    void Update()
    {
        if (Mouse.current == null) return;

        CameraMove();
        CameraZoom();
        ApplyLimits();

        cam.transform.position = targetPosition;
    }
    
    /// <summary>
    /// Configure la caméra avec les dimensions de la map et centre la vue sur la position de départ du joueur.
    /// </summary>
    public void SetUpCamera(int width, int height, int targetX, int targetZ)
    {
        this.mapWidth = width;
        this.mapHeight = height;

        if (cam == null) cam = GetComponent<Camera>();

        cam.orthographic = true;
        cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);

        Vector3 playerBasePosition = new Vector3(targetX, 0f, mapHeight - 1 - targetZ);

        float distance = 20f / Mathf.Abs(cam.transform.forward.y);

        targetPosition = playerBasePosition - (cam.transform.forward * distance);
        targetPosition.y = 20f;

        ApplyLimits();
        cam.transform.position = targetPosition;
        
    }
    
    /// <summary>
    /// Calcule et applique le déplacement de la caméra en fonction de la position du curseur et des touches.
    /// </summary>
    void CameraMove()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        float moveX = 0f;
        float moveZ = 0f;

        if (mousePos.x > Screen.width - boundary || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveX = 1f;
        else if (mousePos.x < boundary || Input.GetKey(isAZERTY ? KeyCode.Q : KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveX = -1f;

        if (mousePos.y > Screen.height - boundary || Input.GetKey(isAZERTY ? KeyCode.Z : KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveZ = 1f;
        else if (mousePos.y < boundary || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveZ = -1f;

        float currentSpeed = (moveX != 0 && moveZ != 0) ? speed / 1.41f : speed;

        Vector3 forwardAxis = cam.transform.forward;
        forwardAxis.y = 0;
        forwardAxis.Normalize();

        Vector3 rightAxis = cam.transform.right;
        rightAxis.y = 0;
        rightAxis.Normalize();

        Vector3 moveDirection = (forwardAxis * moveZ + rightAxis * moveX).normalized;
        targetPosition += moveDirection * currentSpeed * Time.deltaTime;
    }
    
    /// <summary>
    /// Gère le zoom orthographique de la caméra via la molette de la souris.
    /// </summary>
    void CameraZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0 && cam.orthographicSize > 2)
            cam.orthographicSize -= 1;
        else if (scroll < 0 && cam.orthographicSize < 10)
            cam.orthographicSize += 1;
    }
    
    /// <summary>
    /// Applique des limites à la targetPosition en fonction du niveau de zoom et de la taille de la map.
    /// </summary>
    private void ApplyLimits()
    {
        float minX = 0f, maxX = 0f;
        float minZ = 0f, maxZ = 0f;

        switch (Mathf.RoundToInt(cam.orthographicSize))
        {
            case 2:
                minX = -25f; maxX = mapWidth - 25f; 
                minZ = -25f; maxZ = mapHeight - 25f; 
                break;
            case 3:
                minX = -23f; maxX = mapWidth - 27f;
                minZ = -23f; maxZ = mapHeight - 27f;
                break;
            case 4:
                minX = -21f; maxX = mapWidth - 29f;
                minZ = -21f; maxZ = mapHeight - 29f;
                break;
            case 5:
                minX = -19f; maxX = mapWidth - 31f;
                minZ = -19f; maxZ = mapHeight - 31f;
                break;
            case 6:
                minX = -18f; maxX = mapWidth - 32f;
                minZ = -18f; maxZ = mapHeight - 32f;
                break;
            case 7:
                minX = -17f; maxX = mapWidth - 33f;
                minZ = -17f; maxZ = mapHeight - 33f;
                break;
            case 8:
                minX = -16f; maxX = mapWidth - 34f;
                minZ = -16f; maxZ = mapHeight - 34f;
                break;
            case 9:
                minX = -15f; maxX = mapWidth - 35f;
                minZ = -15f; maxZ = mapHeight - 35f;
                break;
            case 10:
                minX = -14f; maxX = mapWidth - 36f; 
                minZ = -14f; maxZ = mapHeight - 36f; 
                break;
                
            default:
                minX = -18f; maxX = mapWidth - 32f;
                minZ = -18f; maxZ = mapHeight - 32f;
                break;
        }

        maxX = Mathf.Max(minX, maxX);
        maxZ = Mathf.Max(minZ, maxZ);

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
    }
}