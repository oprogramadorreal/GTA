using Mirror;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LobbyMenu : MonoBehaviour
{
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[4];

    private void OnEnable()
    {
        GtaNetworkManager.ClientOnConnected += HandleClientConnected;
        PlayerCandidate.ClientOnUpdated += HandleClientPlayerCandidatesUpdated;
    }

    private void OnDisable()
    {
        GtaNetworkManager.ClientOnConnected -= HandleClientConnected;
        PlayerCandidate.ClientOnUpdated -= HandleClientPlayerCandidatesUpdated;
    }

    private void HandleClientConnected()
    {
        startGameButton.gameObject.SetActive(GtaNetworkManager.ClientIsTheHost());
        lobbyUI.SetActive(true);
    }

    private void HandleClientPlayerCandidatesUpdated()
    {
        var players = FindObjectsOfType<PlayerCandidate>()
            .OrderBy(c => c.GetConnectionNumber())
            .ToList();

        var i = 0;

        for (; i < players.Count(); ++i)
        {
            var text = players[i].GetPlayerName();

            if (players[i].hasAuthority)
            {
                text += " (You)";
            }

            playerNameTexts[i].text = text;
        }

        for (; i < 4; ++i)
        {
            playerNameTexts[i].text = "...";
        }
    }

    public void LeaveLobby()
    {
        if (GtaNetworkManager.ClientIsTheHost())
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
            SceneManager.LoadScene(0);
        }
    }

    public void StartGame()
    {
        FindObjectsOfType<PlayerCandidate>()
            .First(p => p.IsTheHost())
            .CmdStartGame();
    }
}
