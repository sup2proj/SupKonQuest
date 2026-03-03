using UnityEngine;
[AddComponentMenu("RTS/Camera Controller")]
public class RTSCamera : MonoBehaviour
{
    [Header("Déplacement")]
    public float moveSpeed = 50f;
    public bool isAZERTY = true; // Coché pour ZQSD, Décoché pour WASD

    [Header("Sensibilité & Rotation")]
    [Range(0.1f, 2f)] 
    public float mouseSensitivity = 0.8f;
    public float rotateSpeed = 30f;
    public float tiltSpeed = 30f;

    [Header("Limites d'Angle (Pitch)")]
    public float minPitch = 20f; // Vue rasante
    public float maxPitch = 40f; // Vue de dessus

    [Header("Limites de Zoom")]
    public float zoomSpeed = 1000f;
    public float minY = 5f;
    public float maxY = 20f; // Limite pour ne pas voir trop de "vide" autour de la map

    private Vector2 mapBounds = new Vector2(100, 100); // Valeur de secours
    private float currentPitch = 60f;
    private bool hasFoundMap = false;

    void Start()
    {
        // On initialise l'angle actuel pour éviter un saut de caméra au premier clic
        currentPitch = transform.eulerAngles.x;
        TryFindMap();
    }

    void Update()
    {
        // Si la map n'est pas encore prête (AutoLauncher), on continue de chercher
        if (!hasFoundMap)
        {
            TryFindMap();
        }

        Move();
        RotateAndTilt();
        Zoom();
    }

    void TryFindMap()
    {
        MapGenerator gen = Object.FindAnyObjectByType<MapGenerator>();
        if (gen != null && gen.mapLayout != null)
        {
            float w = gen.mapLayout.width * gen.tileSize;
            float h = gen.mapLayout.height * gen.tileSize;
            mapBounds = new Vector2(w, h);
            
            // On centre la caméra une seule fois au démarrage
            transform.position = new Vector3(w / 2, 40f, h / 2);
            hasFoundMap = true;
            Debug.Log("✅ Caméra : Map détectée. Limites fixées à " + w + "x" + h);
        }
    }

    void Move()
    {
        float x = 0;
        float z = 0;

        // Détection hybride (Touches + Flèches)
        if (Input.GetKey(isAZERTY ? KeyCode.Z : KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z = 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z = -1;
        if (Input.GetKey(isAZERTY ? KeyCode.Q : KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x = 1;

        if (x != 0 || z != 0)
        {
            // Mouvement relatif à l'orientation de la caméra (ignorer l'axe Y)
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            Vector3 direction = (forward * z + right * x).normalized;
            Vector3 nextPosition = transform.position + direction * moveSpeed * Time.deltaTime;

            // --- BLOCAGE DANS LE MONDE ---
            float margin = 5f; // Petite marge pour voir le bord des tuiles
            nextPosition.x = Mathf.Clamp(nextPosition.x, -margin, mapBounds.x + margin);
            nextPosition.z = Mathf.Clamp(nextPosition.z, -margin, mapBounds.y + margin);

            transform.position = nextPosition;
        }
    }

    void RotateAndTilt()
    {
        if (Input.GetMouseButton(1)) // Clic Droit maintenu
        {
            // Rotation Horizontale (Y)
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * mouseSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up * mouseX, Space.World);

            // Rotation Verticale (X) avec Clamp
            float mouseY = Input.GetAxis("Mouse Y") * tiltSpeed * mouseSensitivity * Time.deltaTime;
            currentPitch -= mouseY; 
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            // Application de la rotation finale sans toucher au Z (roulis)
            transform.localEulerAngles = new Vector3(currentPitch, transform.localEulerAngles.y, 0);
        }
    }

    void Zoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 pos = transform.position;
            pos.y -= scroll * zoomSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }
    }
}