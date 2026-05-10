using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Réglages")]
    public float moveSpeed = 5f;

    void Update()
    {
        if (!IsOwner)
        {
            return; 
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveX, 0f, moveZ);
        
        transform.Translate(movement * moveSpeed * Time.deltaTime);
    }
}