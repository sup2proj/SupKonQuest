using UnityEngine;
using UnityEngine.InputSystem;

public class cameraMouvement : MonoBehaviour
{
    public Vector3 position = new Vector3(0, 0, -10);
    public int boundary = 50;
    public float speed = 10f;

    private int screenBoundsWidth;
    private int screenBoundsHeight;

    private int mapWidth;
    private int mapHeight;

    public bool isAZERTY = true;
    
    void Start()
    {
        screenBoundsWidth = Screen.width;
        screenBoundsHeight = Screen.height;
    }

    void Update()
    {
        Camera.main.orthographic = true;
        if (Mouse.current == null) return;

        CameraMove();
        CameraZoom();
        
        if (position.x > 10) position.x = 10;
        if (position.x <-30) position.x = -30;
        if (position.y > 30) position.y = 30;
        if (position.y <10) position.y = 10;

        Camera.main.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
        
        Camera.main.transform.position = position;
    }

    void SetUpCamera(int mapWidth, int mapHeight)
    {
        mapWidth = mapWidth;
        mapHeight = mapHeight;
        
    }
    
    void CameraMove()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mousePos.x > screenBoundsWidth - boundary || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            position.x += speed * Time.deltaTime;

        if (mousePos.x < boundary || Input.GetKey(isAZERTY ? KeyCode.Q : KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            position.x -= speed * Time.deltaTime;

        if (mousePos.y > screenBoundsHeight - boundary || Input.GetKey(isAZERTY ? KeyCode.Z : KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            position.y += speed * Time.deltaTime;

        if (mousePos.y < boundary || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            position.y -= speed * Time.deltaTime;

    }
    void CameraZoom()
    {
        Vector2 mouseScroll = Mouse.current.scroll.ReadValue();
        if (mouseScroll.y > 0 && Camera.main.orthographicSize > 2)
            Camera.main.orthographicSize -= 1;

        if (mouseScroll.y < 0 && Camera.main.orthographicSize < 10)
            Camera.main.orthographicSize += 1;

    }

}