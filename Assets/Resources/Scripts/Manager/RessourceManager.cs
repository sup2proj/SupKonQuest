using UnityEngine;

public class RessourceManager : MonoBehaviour
{
    [SerializeField] private PlayerSession playerSession;

    public bool CheckIdSession(int idSessionSelected)
    {
        return playerSession != null && playerSession.Id == idSessionSelected;
    }

    public int GetGold()
    {
        return playerSession != null ? playerSession.Gold : 0;
    }
}