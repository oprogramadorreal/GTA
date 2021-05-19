using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class JoinLobbyMenu : MonoBehaviour
{
    [SerializeField] private GameObject landingPagePanel;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private Button joinButton;

    private void OnEnable()
    {
        GtaNetworkManager.ClientOnConnected += HandleClientConnected;
        GtaNetworkManager.ClientOnDisconnected += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        GtaNetworkManager.ClientOnConnected -= HandleClientConnected;
        GtaNetworkManager.ClientOnDisconnected -= HandleClientDisconnected;
    }

    public void Join()
    {
        ((GtaNetworkManager)NetworkManager.singleton).JoinLobby(addressInput.text);

        joinButton.interactable = false;
    }

    private void HandleClientConnected()
    {
        joinButton.interactable = true;

        gameObject.SetActive(false);
        landingPagePanel.SetActive(false);
    }

    private void HandleClientDisconnected()
    {
        joinButton.interactable = true;
    }
}
