using Mirror;
using Steamworks;
using UnityEngine;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameObject landingPagePanel;

    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> lobbyEntered;

    private AudioSource introMusic;

    private void Start()
    {
        introMusic = audioManager.CreateAudioSource("Intro");

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        GameConfigurator.SetMouseCursorVisible(true);
    }

    private void OnDisable()
    {
        if (introMusic != null)
        {
            introMusic.Stop();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void HostLobby()
    {
        landingPagePanel.SetActive(false);

        try
        {
            ((GtaNetworkManager)NetworkManager.singleton).HostLobby();
        }
        catch
        {
            landingPagePanel.SetActive(true);
        }
    }

    public void HostSteamLobby()
    {
        landingPagePanel.SetActive(false);

        try
        {
            ((GtaNetworkManager)NetworkManager.singleton).HostSteamLobby(); // will call OnLobbyCreated when done
        }
        catch
        {
            landingPagePanel.SetActive(true);
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            landingPagePanel.SetActive(true);
        }
        else
        {
            NetworkManager.singleton.StartHost();

            SteamMatchmaking.SetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                "HostAddress",
                SteamUser.GetSteamID().ToString()
            );
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (!NetworkServer.active)
        {
            var hostAddress = SteamMatchmaking.GetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                "HostAddress"
            );

            ((GtaNetworkManager)NetworkManager.singleton).JoinSteamLobby(hostAddress);

            landingPagePanel.SetActive(false);
        }
    }
}
