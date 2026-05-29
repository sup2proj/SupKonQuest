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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestSpawnUnitServerRpc(int playerId, UnitsType type, float x, float z, bool isPoweredUnit, bool isProtector)
    {
        Debug.Log($"[NetworkSpawner] L'hôte a reçu la demande du joueur {playerId} pour créer {type}");
        
        StructureManager.Instance.SpawnUnitByTypeAtPosition(playerId, type, x, z, isPoweredUnit, isProtector, null);
    }
}