using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerMenuManager : MonoBehaviour
{
    public void GoToCreateLobby()
    {
        LobbyRoomManager.IsHost = true;
        SceneManager.LoadScene("LobbyRoomScene");
    }

    public void GoToJoinLobby()
    {
        SceneManager.LoadScene("JoinLobbyScene"); 
    }

    public void BackToLogin()
    {
        SceneManager.LoadScene("LoginScene"); 
    }
}