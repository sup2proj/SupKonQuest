using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Contrôle l'affichage d'un joueur spécifique dans la liste de la salle d'attente (nom, statut prêt/non prêt, et bouton d'expulsion).
/// </summary>
public class PlayerListItem : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public Button kickButton;
    
    private string myPlayerId; 

    /// <summary>
    /// Initialise les informations visuelles du joueur sur l'interface. Colore le pseudo en vert s'il est prêt, et affiche le bouton d'expulsion uniquement si l'utilisateur local est l'hôte et qu'il ne s'agit pas de lui-même.
    /// </summary>
    /// <param name="id">L'identifiant unique (ID) du joueur sur les serveurs d'Unity.</param>
    /// <param name="playerName">Le pseudo du joueur à afficher à l'écran.</param>
    /// <param name="isReady">Vrai si le joueur a cliqué sur le bouton "Prêt", sinon faux.</param>
    /// <param name="isHostView">Vrai si la personne qui regarde cet écran est l'hôte du salon.</param>
    /// <param name="isMe">Vrai si cette ligne représente la personne qui regarde l'écran (pour éviter qu'elle puisse s'expulser elle-même).</param>
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

    /// <summary>
    /// Fonction appelée lorsque l'hôte clique sur le bouton d'expulsion de cette ligne. Demande au LobbyRoomManager d'exclure ce joueur du salon.
    /// </summary>
    public void OnKickClicked()
    {
        FindAnyObjectByType<LobbyRoomManager>().KickPlayer(myPlayerId);
    }
}