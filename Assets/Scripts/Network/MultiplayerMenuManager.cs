using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerMenuManager : MonoBehaviour
{
    public void GoToCreateLobby()
    {
        SceneManager.LoadScene("CreateLobbyScene");
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