using UnityEngine;
using Unity.Netcode;

public class NetworkSpawner : NetworkBehaviour
{
    public static NetworkSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Le client appelle cette fonction, mais elle s'exécute sur hôte
    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnUnitServerRpc(int playerId, UnitsType type, float x, float z, bool isPoweredUnit, bool isProtector)
    {
        Debug.Log($"[NetworkSpawner] L'hôte a reçu la demande du joueur {playerId} pour créer {type}");
        
        //hôte qui appelle sa propre fonction d'instanciation
        StructureManager.Instance.SpawnUnitByTypeAtPosition(playerId, type, x, z, isPoweredUnit, isProtector, null);
    }
}