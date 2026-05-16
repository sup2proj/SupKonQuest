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
        
        playerNameText.text = "- " + playerName + (isReady ? " <color=#00FF00>[PRÊT]</color>" : "");

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