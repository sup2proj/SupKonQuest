using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public Button kickButton;
    
    private string myPlayerId; 

    public void Setup(string id, string playerName, bool isReady, bool isHostView, bool isMe)
    {
        myPlayerId = id;
        
        if (isReady)
        {
            playerNameText.text = "<color=#00FF00>- " + playerName + "</color>";
        }
        else
        {
            playerNameText.text = "- " + playerName;
        }

        if (isHostView && !isMe)
        {
            kickButton.gameObject.SetActive(true);
        }
        else
        {
            kickButton.gameObject.SetActive(false);
        }
    }

    public void OnKickClicked()
    {
        FindAnyObjectByType<LobbyRoomManager>().KickPlayer(myPlayerId);
    }
}