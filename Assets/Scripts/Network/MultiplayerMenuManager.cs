using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MultiplayerMenuManager : MonoBehaviour
{
    [Header("Interface")]
    public TextMeshProUGUI statusText;

    void Start()
    {
        if (PlayerPrefs.HasKey("DisconnectReason"))
        {
            string errorMessage = PlayerPrefs.GetString("DisconnectReason");
            
            if (statusText != null)
            {
                statusText.text = "<color=red>" + errorMessage + "</color>";
            }
            PlayerPrefs.DeleteKey("DisconnectReason");
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "Bienvenue dans le menu multijoueur.";
            }
        }
    }
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