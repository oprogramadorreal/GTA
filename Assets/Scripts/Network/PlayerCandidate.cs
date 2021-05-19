using Mirror;
using System;

public sealed class PlayerCandidate : NetworkBehaviour
{
    [SyncVar]
    private bool isTheHost = false;

    [SyncVar]
    private int connectionId = 0;

    [SyncVar]
    private int connectionNumber = 0;

    public static event Action ClientOnUpdated;

    [ClientCallback]
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        ClientOnUpdated?.Invoke();
    }

    public override void OnStartServer()
    {
        DontDestroyOnLoad(gameObject);
    }

    [ClientCallback]
    private void OnDestroy()
    {
        ClientOnUpdated?.Invoke();
    }

    public override void OnStopClient()
    {
        ClientOnUpdated?.Invoke();
    }

    [Command]
    public void CmdStartGame()
    {
        if (isTheHost)
        {
            ((GtaNetworkManager)NetworkManager.singleton).ServerStartGame();
        }
    }

    public bool IsTheHost()
    {
        return isTheHost;
    }

    [Server]
    public void SetConnectionInfo(int id, int number)
    {
        connectionId = id;
        connectionNumber = number;

        isTheHost = connectionNumber == 1;
    }

    public string GetPlayerName()
    {
        return $"Player {connectionNumber}";
    }

    public int GetConnectionId()
    {
        return connectionId;
    }

    public int GetConnectionNumber()
    {
        return connectionNumber;
    }
}
