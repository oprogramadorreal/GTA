using Mirror;
using TMPro;
using UnityEngine;

public sealed class NameDisplay : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleDisplayNameUpdated))]
    [SerializeField]
    private string displayName = "";

    [SerializeField]
    private TMP_Text displayNameText;

    [Server]
    public void SetDisplayName(string newDisplayName)
    {
        displayName = newDisplayName;
    }

    [Client]
    private void HandleDisplayNameUpdated(string oldName, string newName)
    {
        displayNameText.text = newName;
    }

    public string GetDisplayName()
    {
        return displayName;
    }
}
