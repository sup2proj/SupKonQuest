using UnityEngine;

public class PlayerInstance : MonoBehaviour
{
    public int Gold { get; private set; }
    public int UnitCount { get; private set; }
    public int StructureCount { get; private set; }
    public int Id { get; private set; }

    private PlayerSession playerSession;

    public void Init(PlayerSession session)
    {
        playerSession = session;
        SyncFromSession();
    }

    public void SyncFromSession()
    {
        if (playerSession == null) return;

        Id = playerSession.Id;
        Gold = playerSession.Gold;
        UnitCount = playerSession.UnitCount;
        StructureCount = playerSession.StructureCount;
    }
}
