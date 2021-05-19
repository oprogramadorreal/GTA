using Mirror;
using UnityEngine;

/// <summary>
/// https://stackoverflow.com/questions/31359668/unity-5-1-networking-spawn-an-object-as-a-child-for-the-host-and-all-clients
/// https://mirror-networking.com/docs/Articles/General/Deprecations.html?q=FindLocalObject
/// </summary>
public sealed class NetworkParentingManager : NetworkBehaviour
{
    [SyncVar]
    private uint parentNetId = 0u;

    public override void OnStartClient()
    {
        // When we are spawned on the client,
        // find the parent object using its ID,
        // and set it to be our transform's parent.
        SetParentImpl(parentNetId, false);
    }

    [Server]
    public void OnServerSetParentNetId(uint parentNetId)
    {
        this.parentNetId = parentNetId;
    }

    [Client]
    public void OnClientSetParent(Transform parentTransform, uint newParentNetId)
    {
        // set parent in this client
        transform.SetParent(parentTransform, true);

        // set parent on the other clients
        CmdSetParentOnRemoteClients(newParentNetId);
    }

    [Command]
    private void CmdSetParentOnRemoteClients(uint newParentNetId)
    {
        parentNetId = newParentNetId;

        if (isServerOnly)
        {
            SetParentImpl(newParentNetId, true);
        }

        RpcSetParentOnRemoteClients(newParentNetId);
    }

    /// <summary>
    /// This is called in all clients (except in the owner client) from
    /// CmdSetParentOnRemoteClients command in the server that, in turn,
    /// was called by SetParent in the owner client. Parent was already
    /// set at SetParent in the owner parent, so that why 'includeOwner' flag is set here.
    /// </summary>
    [ClientRpc(includeOwner = false)]
    private void RpcSetParentOnRemoteClients(uint newParentNetId)
    {
        Debug.Assert(!isLocalPlayer); // local player already had its parent set
        SetParentImpl(newParentNetId, true);
    }

    private void SetParentImpl(uint newParentNetId, bool worldPositionStays)
    {
        Transform parentTransform = null;

        if (NetworkIdentity.spawned.TryGetValue(newParentNetId, out var networkIdentity))
        {
            parentTransform = networkIdentity.gameObject.transform;
        }

        transform.SetParent(parentTransform, worldPositionStays);
    }
}
