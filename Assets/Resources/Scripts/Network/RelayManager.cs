using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using TMPro;

/// <summary>
/// Gère la création et la connexion aux serveurs d'Unity (Relay) en utilisant Netcode for GameObjects. Fait le lien entre le matchmaking et le trafic réseau réel du jeu.
/// </summary>
public class RelayManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI displayedCodeText;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    /// <summary>
    /// Alloue un serveur Relay pour héberger la partie (jusqu'à 8 joueurs au total), récupère le code de connexion unique, configure le transport réseau (UnityTransport) et lance le jeu en tant qu'Hôte (Host).
    /// </summary>
    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            displayedCodeText.text = "Code: " + joinCode;
            Debug.Log("Code créé : " + joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Tente de rejoindre un serveur Relay existant en utilisant le code saisi dans l'interface, configure le transport réseau (UnityTransport) et connecte le joueur au jeu en tant que Client.
    /// </summary>
    public async void JoinRelay()
    {
        try
        {
            string joinCode = joinCodeInput.text;
            Debug.Log("Tentative de rejoindre avec : " + joinCode);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
}