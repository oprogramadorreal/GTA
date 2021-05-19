using kcp2k;
using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("")]
public sealed class GtaNetworkManager : NetworkManager
{
    [SerializeField]
    private GameObject playerCanidatePrefab;

    [SerializeField]
    private KcpTransport kcpTransportComponent;

    [SerializeField]
    private FizzySteamworks fizzyTransportComponent;

    private bool isGameInProgress = false;

    public static event Action ClientOnConnected;
    public static event Action ClientOnDisconnected;

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        if (isGameInProgress)
        {
            //conn.Disconnect(); // not allow joining while game is already in progress
        }
        else
        {
            // still at the lobby
            SpawnCurrentPlayerCandidates();            
        }

        SpawnNewPlayerCandidate(conn);
    }

    private static void SpawnCurrentPlayerCandidates()
    {
        foreach (var c in FindObjectsOfType<PlayerCandidate>())
        {
            NetworkServer.Spawn(c.gameObject, c.netIdentity.connectionToClient); // keep original authority
        }
    }

    private void SpawnNewPlayerCandidate(NetworkConnection conn)
    {
        var obj = Instantiate(playerCanidatePrefab);

        var candidate = obj.GetComponent<PlayerCandidate>();
        candidate.SetConnectionInfo(conn.connectionId, NetworkServer.connections.Count);

        NetworkServer.Spawn(obj, conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        isGameInProgress = false;
    }

    public void ServerStartGame()
    {
        isGameInProgress = true;
        ServerChangeScene("MainScene");
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        if (IsInMainScene())
        {
            ClientScene.AddPlayer(conn);
        }
    }

    private static bool IsInMainScene()
    {
        return SceneManager.GetActiveScene().name.StartsWith("MainScene");
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);

        var nameDisplay = conn.identity.GetComponent<NameDisplay>();

        if (nameDisplay != null)
        {
            var playerCandidate = FindPlayerCandidate(conn);
            var playerName = playerCandidate.GetPlayerName();

            nameDisplay.SetDisplayName(playerName);

            NetworkServer.Destroy(playerCandidate.gameObject);
        }
    }

    private static PlayerCandidate FindPlayerCandidate(NetworkConnection conn)
    {
        return FindObjectsOfType<PlayerCandidate>()
            .FirstOrDefault(p => p.GetConnectionId() == conn.connectionId);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        ClientOnConnected?.Invoke();
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        ClientOnDisconnected?.Invoke();
    }

    public static bool ClientIsTheHost()
    {
        return NetworkServer.active
            && NetworkClient.isConnected;
    }

    public void HostLobby()
    {
        SetupTransport_Kcp();
        StartHost();
    }

    public void HostSteamLobby()
    {
        SetupTransport_Steamworks();
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
    }

    public void JoinSteamLobby(string hostAddress)
    {
        SetupTransport_Steamworks();
        networkAddress = hostAddress;
        StartClient();
    }

    public void JoinLobby(string hostAddress)
    {
        SetupTransport_Kcp();
        networkAddress = hostAddress;
        StartClient();
    }

    private void SetupTransport_Steamworks()
    {
        fizzyTransportComponent.enabled = true;
        transport = fizzyTransportComponent;
        Transport.activeTransport = transport;

        kcpTransportComponent.enabled = false;
    }

    private void SetupTransport_Kcp()
    {
        kcpTransportComponent.enabled = true;
        transport = kcpTransportComponent;
        Transport.activeTransport = transport;

        fizzyTransportComponent.enabled = false;
    }
}
