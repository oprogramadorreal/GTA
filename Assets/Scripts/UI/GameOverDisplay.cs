using Mirror;
using TMPro;
using UnityEngine;

public sealed class GameOverDisplay : MonoBehaviour
{
    [SerializeField] private GameObject parent;
    [SerializeField] private TMP_Text message;

    private AudioManager audioManager;

    private void Start()
    {
        audioManager = FindObjectOfType<AudioManager>();
    }

    public void TryAgain()
    {
        audioManager.CreateTemporaryAudioSource("ButtonClicked");

        var playerController = ClientScene.localPlayer.gameObject.GetComponentInChildren<PlayerController>();
        playerController.Revive();
        parent.SetActive(false);
        GameConfigurator.SetMouseCursorVisible(false);
    }

    public void LeaveGame()
    {
        if (GtaNetworkManager.ClientIsTheHost())
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }

    public void ShowGameOver(string killerName)
    {
        message.text = $"You were killed by {killerName}";
        parent.SetActive(true);
        GameConfigurator.SetMouseCursorVisible(true);
    }
}
