using Cinemachine;
using Mirror;
using UnityEngine;

public sealed class GameConfigurator : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private Transform playerSpawnSpot;

    [SerializeField]
    private GameObject enterCarMessage;

    [SerializeField]
    private AudioManager audioManager;

    [SerializeField]
    private CameraManager cameraManager;

    [SerializeField]
    private CinemachineFreeLook playerCamera;

    [SerializeField]
    private CinemachineVirtualCamera globalCamera;

    [SerializeField]
    private GameObject leaveGameDisplay;

    [ClientCallback]
    private void Awake()
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
        }
    }

    [ClientCallback]
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetMouseCursorVisible(!leaveGameDisplay.activeInHierarchy);
            leaveGameDisplay.SetActive(!leaveGameDisplay.activeInHierarchy);
        }
    }

    [Client]
    public void ClientSetupPlayer(GameObject newPlayer)
    {
        newPlayer.GetComponent<PlayerController>().SetCameraTransform(cameraTransform);
        newPlayer.GetComponentInChildren<Billboard>().SetCameraTransform(cameraTransform);
        newPlayer.GetComponent<PlayerCarController>().ClientSetup(cameraManager, enterCarMessage);
        newPlayer.GetComponent<PlayerSfxs>().ClientSetup(audioManager);
    }

    [Client]
    public void LocalPlayerStarted(GameObject newPlayer)
    {
        newPlayer.GetComponent<PlayerController>().ClientSetupCamera(playerCamera);

        SetMouseCursorVisible(false);
        globalCamera.Priority = 0;
    }

    public void LocalPlayerDestroyed()
    {
        globalCamera.Priority = 11;
    }

    [Client]
    public static void SetMouseCursorVisible(bool visible)
    {
        Cursor.visible = visible;

        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
